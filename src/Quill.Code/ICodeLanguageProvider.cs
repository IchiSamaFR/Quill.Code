namespace Quill.Code;

/// <summary>
/// Fournisseur de services d'éditeur pour une langue de code source.
/// </summary>
public interface ICodeLanguageProvider : ICodeCompletion, ICodeDiagnostic, ICodeSignature, ICodeHover
{
    /// <summary>
    /// Langue du fournisseur.
    /// </summary>
    string Language { get; }
}
