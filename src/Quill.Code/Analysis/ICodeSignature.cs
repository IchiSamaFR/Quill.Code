
namespace Quill.Code.Analysis;

/// <summary>
/// Typing assistance source of an editor, on raw text: decouples the control from the engine (Roslyn…).
/// </summary>
public interface ICodeSignature
{
    /// <summary>Suggestions valid at <paramref name="position"/> in <paramref name="code"/>.</summary>
    Task<SignatureHelpInfo?> GetSignatureHelpAsync(string code, int position, CancellationToken cancellationToken = default);
}
