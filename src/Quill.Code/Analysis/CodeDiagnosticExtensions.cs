using Quill.Code.Text;

namespace Quill.Code.Analysis;

/// <summary>Présentation des diagnostics.</summary>
public static class CodeDiagnosticExtensions
{
    /// <summary>
    /// Ligne de journal : « Ligne 2, colonne 6 : CS1061 message » (base 1), <paramref name="text"/> étant le texte
    /// sur lequel portent les positions.
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
