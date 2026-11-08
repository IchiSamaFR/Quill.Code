using Quill.Code.Constants;

namespace Quill.Code.Highlighting;

/// <summary>
/// Highlighting definition of a language, read from a .xshd file (<see cref="XshdReader"/>).
/// Immutable: the same definition serves all editors, on all threads.
/// </summary>
public sealed class SyntaxDefinition
{
    internal SyntaxDefinition(
        string name,
        IReadOnlyList<string> extensions,
        IReadOnlyDictionary<string, string> properties,
        IReadOnlyDictionary<string, SyntaxColor> colors,
        SyntaxColor? defaultColor,
        IReadOnlyList<SyntaxKeyword> keywords,
        IReadOnlyList<SyntaxRule> rules)
    {
        Name = name;
        Extensions = extensions;
        Properties = properties;
        Colors = colors;
        DefaultColor = defaultColor;
        Keywords = keywords;
        Rules = rules;
    }

    /// <summary>Language name (<c>name</c> attribute).</summary>
    public string Name { get; }

    /// <summary>Associated extensions (e.g. ".sql"), as declared in the .xshd.</summary>
    public IReadOnlyList<string> Extensions { get; }

    /// <summary><c>Property</c> elements (name → value), with a case-insensitive name.</summary>
    public IReadOnlyDictionary<string, string> Properties { get; }

    /// <summary>Named colors, with a case-insensitive name.</summary>
    public IReadOnlyDictionary<string, SyntaxColor> Colors { get; }

    /// <summary>Formatting of text no rule recognizes ("Default" color), or null.</summary>
    public SyntaxColor? DefaultColor { get; }

    /// <summary>Words of the <c>Keywords</c> lists, in file order.</summary>
    public IReadOnlyList<SyntaxKeyword> Keywords { get; }

    /// <summary>Line comment prefix (<c>LineComment</c> property), or null.</summary>
    public string? LineComment => Properties.TryGetValue(HighlightingConstants.LineCommentProperty, out var value) && value.Length > 0 ? value : null;

    /// <summary>Rules in file order: at the same position, the first one wins.</summary>
    internal IReadOnlyList<SyntaxRule> Rules { get; }
}
