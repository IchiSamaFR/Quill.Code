namespace Quill.Code.Editing;

/// <summary>Règles de commentaires et de chaînes d'un langage, pour distinguer le code du reste (<see cref="SourceScanner"/>).</summary>
public sealed record SourceSyntax
{
    /// <summary>C# : // et /* */, "..." et '...' avec \, @"..." et $@"..." avec "" doublé, """...""" brut.</summary>
    public static SourceSyntax CSharp { get; } = new()
    {
        LineComment = "//",
        BackslashEscapes = true,
        VerbatimStrings = true,
        RawStrings = true,
        StringsEndAtLineEnd = true,
    };

    /// <summary>T-SQL : -- et /* */, '...' et "..." avec le délimiteur doublé.</summary>
    public static SourceSyntax Sql { get; } = new()
    {
        LineComment = "--",
        DoubledQuoteEscapes = true,
    };

    /// <summary>Préfixe de commentaire de ligne, null si le langage n'en a pas.</summary>
    public string? LineComment { get; init; }

    /// <summary>Début de commentaire de bloc, null si le langage n'en a pas.</summary>
    public string? BlockCommentStart { get; init; } = "/*";

    /// <summary>Fin de commentaire de bloc.</summary>
    public string? BlockCommentEnd { get; init; } = "*/";

    /// <summary>\ échappe le caractère suivant dans une chaîne.</summary>
    public bool BackslashEscapes { get; init; }

    /// <summary>Le délimiteur doublé ('' ou "") est un caractère de la chaîne.</summary>
    public bool DoubledQuoteEscapes { get; init; }

    /// <summary>Chaînes verbatim C# (@"...", $@"...") : pas d'échappement par \, "" doublé, plusieurs lignes.</summary>
    public bool VerbatimStrings { get; init; }

    /// <summary>Chaînes brutes C# ("""...""") : fermées par autant de guillemets qu'à l'ouverture.</summary>
    public bool RawStrings { get; init; }

    /// <summary>Une chaîne non fermée s'arrête en fin de ligne (sauf verbatim et brute).</summary>
    public bool StringsEndAtLineEnd { get; init; }
}
