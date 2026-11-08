namespace Quill.Code.Editing;

/// <summary>Settings for the editing commands (<see cref="CodeEditing"/>).</summary>
public sealed record CodeEditingOptions
{
    /// <summary>Width of one indentation level, in spaces.</summary>
    public int IndentSize { get; init; } = 4;

    /// <summary>Indent with tabs rather than spaces.</summary>
    public bool UseTabs { get; init; }

    /// <summary>Inserted line break ("\n", "\r\n"). Reading the text accepts both.</summary>
    public string NewLine { get; init; } = "\n";

    /// <summary>Comments and strings of the language: pairs and brackets only act in code.</summary>
    public SourceSyntax Syntax { get; init; } = SourceSyntax.CSharp;

    /// <summary>"{" typed at the end of a code line moves to the next line along with its "}" (Allman style).</summary>
    public bool PlaceOpenBraceOnNewLine { get; init; }

    /// <summary>Space inserted after the prefix when commenting ("// x"), removed when uncommenting.</summary>
    public bool InsertSpaceAfterLineComment { get; init; } = true;

    /// <summary>One indentation level.</summary>
    public string IndentUnit => UseTabs ? "\t" : new string(' ', IndentSize);
}
