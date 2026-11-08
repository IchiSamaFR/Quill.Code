namespace Quill.Code.Highlighting;

/// <summary>
/// Formatting of a text category (<c>Color</c> element of a .xshd), as written in the file:
/// each application converts it for its framework (WPF Brush, MAUI Color…).
/// </summary>
/// <param name="Name">Color name ("Comment", "Keyword"…); null for formatting declared on the rule.</param>
/// <param name="Foreground">Text color ("#569CD6", "Green"…); inherited from the editor if null.</param>
/// <param name="FontWeight">Weight ("Bold"…); inherited if null.</param>
/// <param name="FontStyle">Style ("Italic"…); inherited if null.</param>
public sealed record SyntaxColor(string? Name, string? Foreground, string? FontWeight = null, string? FontStyle = null);
