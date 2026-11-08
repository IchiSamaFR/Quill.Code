using Quill.Code.Text;

namespace Quill.Code.Analysis;

/// <summary>Diagnostic formatting.</summary>
public static class CodeDiagnosticExtensions
{
    /// <summary>
    /// Log line: "Line 2, column 6: CS1061 message" (1-based), <paramref name="text"/> being the text
    /// the positions refer to.
    /// </summary>
    public static string Describe(this CodeDiagnostic diagnostic, string text)
    {
        ArgumentNullException.ThrowIfNull(diagnostic);
        ArgumentNullException.ThrowIfNull(text);

        var position = LinePosition.FromOffset(text, diagnostic.Start);
        var code = diagnostic.Code is null ? string.Empty : diagnostic.Code + " ";
        return $"Ligne {position.Line}, colonne {position.Column} : {code}{diagnostic.Message}";
    }
}
