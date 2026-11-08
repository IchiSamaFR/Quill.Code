using Quill.Code.Analysis;

namespace Quill.Code.CSharp.Compilation;

/// <summary>The code passed to <see cref="CSharpLanguageService.RunAsync"/> does not compile: nothing was run.</summary>
public sealed class CSharpCompilationException : Exception
{
    /// <summary>Failed compilation, with all its diagnostics (errors and warnings).</summary>
    public CSharpCompilationException(IReadOnlyList<CodeDiagnostic> diagnostics)
        : base("The code does not compile.")
    {
        ArgumentNullException.ThrowIfNull(diagnostics);
        Diagnostics = diagnostics;
    }

    /// <summary>Errors and warnings, at editor text positions.</summary>
    public IReadOnlyList<CodeDiagnostic> Diagnostics { get; }

    /// <summary>Errors only.</summary>
    public IReadOnlyList<CodeDiagnostic> Errors => Diagnostics.Where(d => d.IsError).ToArray();
}
