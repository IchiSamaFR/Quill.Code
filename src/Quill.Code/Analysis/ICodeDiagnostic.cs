
namespace Quill.Code.Analysis;

/// <summary>Source des diagnostics d'un éditeur, sur le texte brut : découple le contrôle du compilateur.</summary>
public interface ICodeDiagnostic
{
    /// <summary>Erreurs, avertissements et informations de <paramref name="code"/>.</summary>
    Task<IReadOnlyList<CodeDiagnostic>> GetDiagnosticsAsync(string code, CancellationToken cancellationToken = default);
}
