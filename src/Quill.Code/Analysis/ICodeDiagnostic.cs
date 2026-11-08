
namespace Quill.Code.Analysis;

/// <summary>Diagnostic source of an editor, on raw text: decouples the control from the compiler.</summary>
public interface ICodeDiagnostic
{
    /// <summary>Errors, warnings and information for <paramref name="code"/>.</summary>
    Task<IReadOnlyList<CodeDiagnostic>> GetDiagnosticsAsync(string code, CancellationToken cancellationToken = default);
}
