using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Emit;
using Quill.Code.Analysis;
using Quill.Code.CSharp.Features;
using Quill.Code.CSharp.Roslyn;
using System.Globalization;

namespace Quill.Code.CSharp.Compilation;

/// <summary>Emits a document's project and loads the resulting assembly.</summary>
internal static class CSharpCompiler
{
    public static async Task<CSharpCompilationResult> CompileAsync(CSharpDocument document, CSharpCompileOptions options, CancellationToken cancellationToken)
    {
        var compilation = await document.Document.Project.GetCompilationAsync(cancellationToken)
            ?? throw new InvalidOperationException("Roslyn n'a pas produit de compilation pour le projet.");

        // Unique name: each compilation is loaded into its own context.
        var name = options.AssemblyName ?? $"{compilation.AssemblyName}_{Guid.NewGuid():N}";

        using var image = new MemoryStream();
        using var symbols = new MemoryStream();
        var result = compilation.WithAssemblyName(name).Emit(
            image,
            symbols,
            options: new EmitOptions(debugInformationFormat: DebugInformationFormat.PortablePdb),
            cancellationToken: cancellationToken);

        var reported = result.Diagnostics.Where(d => d.Severity is DiagnosticSeverity.Warning or DiagnosticSeverity.Error).ToArray();
        var diagnostics = (await CSharpDiagnosticMapper.MapAsync(document, reported, cancellationToken)).ToList();

        // Errors of the sources added to the project (e.g. helper class): with no position in the editor, the file is named.
        var tree = await document.Document.GetSyntaxTreeAsync(cancellationToken);
        diagnostics.InsertRange(0, reported
            .Where(d => d.Severity == DiagnosticSeverity.Error && d.Location.SourceTree is { FilePath.Length: > 0 } source && source != tree)
            .Select(d => new CodeDiagnostic(0, 0, CodeDiagnosticSeverity.Error, Describe(d), d.Id)));

        if (!result.Success)
        {
            // Error in the code surrounding the text: no useful position, but never silent.
            if (!diagnostics.Any(d => d.IsError) && reported.FirstOrDefault(d => d.Severity == DiagnosticSeverity.Error) is { } error)
                diagnostics.Insert(0, new CodeDiagnostic(0, 0, CodeDiagnosticSeverity.Error, error.GetMessage(CultureInfo.CurrentUICulture), error.Id));

            return new CSharpCompilationResult(diagnostics, null, null);
        }


        var context = new CompiledCodeLoadContext(name, options);
        var bytes = image.ToArray();
        symbols.Position = 0;
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
