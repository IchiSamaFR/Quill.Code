using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Emit;
using Quill.Code.Analysis;
using Quill.Code.CSharp.Analysis;
using Quill.Code.CSharp.Scripting;
using Quill.Code.CSharp.Workspaces;
using System.Globalization;

namespace Quill.Code.CSharp.Compilation;

/// <summary>Émission du projet d'un document et chargement de l'assembly obtenue.</summary>
internal static class CSharpCompiler
{
    public static async Task<CSharpCompilationResult> CompileAsync(CSharpDocument document, CSharpCompileOptions options, CancellationToken cancellationToken)
    {
        var compilation = await document.Document.Project.GetCompilationAsync(cancellationToken)
            ?? throw new InvalidOperationException("Roslyn n'a pas produit de compilation pour le projet.");

        // Nom unique : chaque compilation est chargée dans son propre contexte.
        var name = options.AssemblyName ?? $"{compilation.AssemblyName}_{Guid.NewGuid():N}";

        using var image = new MemoryStream();
        using var symbols = options.EmitDebugInformation ? new MemoryStream() : null;
        var result = compilation.WithAssemblyName(name).Emit(
            image,
            symbols,
            options: symbols is null ? null : new EmitOptions(debugInformationFormat: DebugInformationFormat.PortablePdb),
            cancellationToken: cancellationToken);

        var reported = result.Diagnostics.Where(d => d.Severity is DiagnosticSeverity.Warning or DiagnosticSeverity.Error).ToArray();
        var diagnostics = (await CSharpDiagnosticMapper.MapAsync(document, reported, cancellationToken)).ToList();

        // Erreurs des sources ajoutées au projet (ex. classe utilitaire) : sans position dans l'éditeur, le fichier est cité.
        var tree = await document.Document.GetSyntaxTreeAsync(cancellationToken);
        diagnostics.InsertRange(0, reported
            .Where(d => d.Severity == DiagnosticSeverity.Error && d.Location.SourceTree is { FilePath.Length: > 0 } source && source != tree)
            .Select(d => new CodeDiagnostic(0, 0, CodeDiagnosticSeverity.Error, Describe(d), d.Id)));

        if (!result.Success)
        {
            // Erreur dans le code qui entoure le texte : sans position utile, mais jamais silencieuse.
            if (!diagnostics.Any(d => d.IsError) && reported.FirstOrDefault(d => d.Severity == DiagnosticSeverity.Error) is { } error)
                diagnostics.Insert(0, new CodeDiagnostic(0, 0, CodeDiagnosticSeverity.Error, error.GetMessage(CultureInfo.CurrentUICulture), error.Id));

            return new CSharpCompilationResult(diagnostics, null, null);
        }

        var bytes = image.ToArray();
        if (symbols is not null)
            symbols.Position = 0;

        var context = new CompiledCodeLoadContext(name, options);
        var assembly = context.LoadFromStream(new MemoryStream(bytes), symbols);

        if (options.Log is { } log)
        {
            try
            {
                CSharpScriptRuntime.BindLog(assembly, log);
            }
            catch when (context.IsCollectible)
            {
                context.Unload();
                throw;
            }
        }

        return new CSharpCompilationResult(diagnostics, assembly, context, bytes);
    }

    private static string Describe(Diagnostic diagnostic)
    {
        var position = diagnostic.Location.GetLineSpan().StartLinePosition;
        return $"{Path.GetFileName(diagnostic.Location.SourceTree!.FilePath)} ({position.Line + 1}, {position.Character + 1}) : {diagnostic.GetMessage(CultureInfo.CurrentUICulture)}";
    }
}
