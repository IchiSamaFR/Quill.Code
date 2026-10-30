using Quill.Code.Analysis;

namespace Quill.Code.CSharp.Compilation;

/// <summary>Le code passé à <see cref="CSharpLanguageService.RunAsync"/> ne compile pas : rien n'a été exécuté.</summary>
public sealed class CSharpCompilationException : Exception
{
    /// <summary>Compilation en échec, avec tous ses diagnostics (erreurs et avertissements).</summary>
    public CSharpCompilationException(IReadOnlyList<CodeDiagnostic> diagnostics)
        : base("Le code ne compile pas.")
    {
        ArgumentNullException.ThrowIfNull(diagnostics);
        Diagnostics = diagnostics;
    }

    /// <summary>Erreurs et avertissements, aux positions du texte de l'éditeur.</summary>
    public IReadOnlyList<CodeDiagnostic> Diagnostics { get; }

    /// <summary>Erreurs seules.</summary>
    public IReadOnlyList<CodeDiagnostic> Errors => Diagnostics.Where(d => d.IsError).ToArray();
}
