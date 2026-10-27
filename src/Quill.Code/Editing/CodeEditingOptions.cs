namespace Quill.Code.Editing;

/// <summary>Réglages des commandes d'édition (<see cref="CodeEditing"/>).</summary>
public sealed record CodeEditingOptions
{
    /// <summary>Largeur d'un niveau d'indentation, en espaces.</summary>
    public int IndentSize { get; init; } = 4;

    /// <summary>Indentation par tabulations plutôt que par espaces.</summary>
    public bool UseTabs { get; init; }

    /// <summary>Saut de ligne inséré (« \n », « \r\n »). La lecture du texte accepte les deux.</summary>
    public string NewLine { get; init; } = "\n";

    /// <summary>Commentaires et chaînes du langage : les paires et crochets n'agissent que dans le code.</summary>
    public SourceSyntax Syntax { get; init; } = SourceSyntax.CSharp;

    /// <summary>« { » tapée en fin de ligne de code passe sur la ligne suivante avec sa « } » (style Allman).</summary>
    public bool PlaceOpenBraceOnNewLine { get; init; }

    /// <summary>Espace inséré après le préfixe en commentant (« // x »), retiré en décommentant.</summary>
    public bool InsertSpaceAfterLineComment { get; init; } = true;

    /// <summary>Un niveau d'indentation.</summary>
    public string IndentUnit => UseTabs ? "\t" : new string(' ', IndentSize);
}
