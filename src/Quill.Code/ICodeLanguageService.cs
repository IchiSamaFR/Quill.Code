namespace Quill.Code;

/// <summary>Services d'éditeur d'un langage, sur le texte brut : autocomplétion, diagnostics, survol et signatures.</summary>
public interface ICodeLanguageService : ICodeCompletion, ICodeDiagnostic, ICodeSignature, ICodeHover
{
    /// <summary>Langage du service (voir <see cref="Languages.CodeLanguages"/>).</summary>
    string Language { get; }
}
