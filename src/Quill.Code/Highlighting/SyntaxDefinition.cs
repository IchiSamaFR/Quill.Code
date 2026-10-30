using Quill.Code.Constants;

namespace Quill.Code.Highlighting;

/// <summary>
/// Définition de coloration d'un langage, lue depuis un fichier .xshd (<see cref="XshdReader"/>).
/// Immuable : une même définition sert à tous les éditeurs, sur tous les threads.
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

    /// <summary>Nom du langage (attribut <c>name</c>).</summary>
    public string Name { get; }

    /// <summary>Extensions associées (ex. « .sql »), telles que déclarées dans le .xshd.</summary>
    public IReadOnlyList<string> Extensions { get; }

    /// <summary>Éléments <c>Property</c> (nom → valeur), sans distinction de casse sur le nom.</summary>
    public IReadOnlyDictionary<string, string> Properties { get; }

    /// <summary>Couleurs nommées, sans distinction de casse sur le nom.</summary>
    public IReadOnlyDictionary<string, SyntaxColor> Colors { get; }

    /// <summary>Mise en forme du texte qu'aucune règle ne reconnaît (couleur « Default »), ou null.</summary>
    public SyntaxColor? DefaultColor { get; }

    /// <summary>Mots des listes <c>Keywords</c>, dans l'ordre du fichier.</summary>
    public IReadOnlyList<SyntaxKeyword> Keywords { get; }

    /// <summary>Préfixe de commentaire de ligne (propriété <c>LineComment</c>), ou null.</summary>
    public string? LineComment => Properties.TryGetValue(HighlightingConstants.LineCommentProperty, out var value) && value.Length > 0 ? value : null;

    /// <summary>Règles dans l'ordre du fichier : à position égale, la première l'emporte.</summary>
    internal IReadOnlyList<SyntaxRule> Rules { get; }
}
