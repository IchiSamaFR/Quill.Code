namespace Quill.Code.Editing;

/// <summary>Comment and string rules of a language, to tell code apart from the rest (<see cref="SourceScanner"/>).</summary>
public sealed record SourceSyntax
{
    /// <summary>C#: // and /* */, "..." and '...' with \, @"..." and $@"..." with doubled "", raw """...""".</summary>
    public static SourceSyntax CSharp { get; } = new()
    {
        LineComment = "//",
        BackslashEscapes = true,
        VerbatimStrings = true,
        RawStrings = true,
        StringsEndAtLineEnd = true,
    };

    /// <summary>T-SQL: -- and /* */, '...' and "..." with doubled delimiter.</summary>
    public static SourceSyntax Sql { get; } = new()
    {
        LineComment = "--",
        DoubledQuoteEscapes = true,
    };

    /// <summary>Line comment prefix, null if the language has none.</summary>
    public string? LineComment { get; init; }

    /// <summary>Block comment start, null if the language has none.</summary>
    public string? BlockCommentStart { get; init; } = "/*";

    /// <summary>Block comment end.</summary>
    public string? BlockCommentEnd { get; init; } = "*/";

    /// <summary>\ escapes the next character in a string.</summary>
    public bool BackslashEscapes { get; init; }

    /// <summary>The doubled delimiter ('' or "") is a character of the string.</summary>
    public bool DoubledQuoteEscapes { get; init; }

    /// <summary>C# verbatim strings (@"...", $@"..."): no \ escaping, doubled "", multi-line.</summary>
    public bool VerbatimStrings { get; init; }

    /// <summary>C# raw strings ("""..."""): closed by as many quotes as at the opening.</summary>
    public bool RawStrings { get; init; }

    /// <summary>An unterminated string stops at the end of the line (except verbatim and raw).</summary>
    public bool StringsEndAtLineEnd { get; init; }
}
