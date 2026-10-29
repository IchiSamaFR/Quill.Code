
namespace Quill.Code.Analysis;

/// <summary>
/// Source d'aide à la frappe d'un éditeur, sur le texte brut : découple le contrôle du moteur (Roslyn…).
/// </summary>
public interface ICodeSignature
{
    /// <summary>Propositions valables à <paramref name="position"/> dans <paramref name="code"/>.</summary>
    Task<SignatureHelpInfo?> GetSignatureHelpAsync(string code, int position, CancellationToken cancellationToken = default);
}
