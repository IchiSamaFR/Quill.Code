using Quill.Code.Analysis;
using Quill.Code.Completion;

namespace Quill.Code;

/// <summary>Editor services for a language, on raw text: completion, diagnostics, hover and signatures.</summary>
public interface ICodeLanguageService : ICodeCompletion, ICodeDiagnostic, ICodeSignature, ICodeHover
{
    /// <summary>Language of the service (see <see cref="Languages.CodeLanguages"/>).</summary>
    string Language { get; }
}
