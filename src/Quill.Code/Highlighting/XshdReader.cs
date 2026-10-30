using System.Text.RegularExpressions;
using System.Xml.Linq;
using Quill.Code.Constants;

namespace Quill.Code.Highlighting;

/// <summary>
/// Lecture des définitions de coloration au format .xshd d'AvalonEdit (2008).
/// </summary>
/// <remarks>
/// Pris en charge : <c>Property</c>, <c>Color</c> (foreground, fontWeight, fontStyle), le <c>RuleSet</c> principal avec ses
/// <c>Span</c> (Begin/End, multiline), <c>Rule</c> et <c>Keywords</c>. Les RuleSet imbriqués (échappements dans un span)
/// et les imports sont ignorés : le span est alors coloré d'un seul tenant.
/// </remarks>
public static class XshdReader
{
    private static readonly XNamespace Xshd = "http://icsharpcode.net/sharpdevelop/syntaxdefinition/2008";

    /// <summary>Lit une définition depuis un flux.</summary>
    /// <exception cref="FormatException">Fichier sans RuleSet, couleur sans nom ou non déclarée.</exception>
    public static SyntaxDefinition Load(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        return Load(XDocument.Load(stream));
    }

    /// <inheritdoc cref="Load(Stream)"/>
    public static async Task<SyntaxDefinition> LoadAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        return Load(await XDocument.LoadAsync(stream, LoadOptions.None, cancellationToken));
    }

    /// <summary>Lit une définition depuis son texte XML.</summary>
    public static SyntaxDefinition Parse(string xml)
    {
        ArgumentNullException.ThrowIfNull(xml);
        return Load(XDocument.Parse(xml));
    }

    /// <summary>Lit une définition depuis un document XML déjà chargé.</summary>
    public static SyntaxDefinition Load(XDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        var root = document.Root ?? throw new FormatException("Définition de coloration vide.");
        var colors = ReadColors(root);
        var properties = ReadProperties(root);

        // Le RuleSet principal est celui sans nom.
        var ruleSet = root.Elements(Xshd + "RuleSet").FirstOrDefault(e => e.Attribute("name") is null)
            ?? throw new FormatException("Définition de coloration sans RuleSet principal.");

        var options = RegexOptions.CultureInvariant;
        if ((bool?)ruleSet.Attribute("ignoreCase") == true)
            options |= RegexOptions.IgnoreCase;

        List<SyntaxRule> rules = [];
        List<SyntaxKeyword> keywords = [];
        foreach (var element in ruleSet.Elements())
        {
            var rule = element.Name.LocalName switch
            {
                "Span" => ReadSpan(element, colors, options),
                "Rule" => ReadRule(element, colors, options),
                "Keywords" => ReadKeywords(element, colors, options, keywords),
                _ => null,
            };

            if (rule is not null)
                rules.Add(rule);
        }

        return new SyntaxDefinition(
            (string?)root.Attribute("name") ?? string.Empty,
            ((string?)root.Attribute("extensions"))?.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? [],
            properties,
            colors,
            colors.TryGetValue(HighlightingConstants.DefaultColorName, out var defaultColor) ? defaultColor : null,
            keywords,
            rules);
    }

    private static Dictionary<string, SyntaxColor> ReadColors(XElement root)
    {
        Dictionary<string, SyntaxColor> colors = new(StringComparer.OrdinalIgnoreCase);
        foreach (var element in root.Elements(Xshd + "Color"))
        {
            var name = (string?)element.Attribute("name");
            if (string.IsNullOrEmpty(name))
                throw new FormatException("Couleur sans attribut « name ».");

            colors[name] = ReadColor(element, name);
        }

        return colors;
    }

    private static Dictionary<string, string> ReadProperties(XElement root)
    {
        Dictionary<string, string> properties = new(StringComparer.OrdinalIgnoreCase);
        foreach (var element in root.Elements(Xshd + "Property"))
        {
            if ((string?)element.Attribute("name") is { Length: > 0 } name)
                properties[name] = (string?)element.Attribute("value") ?? string.Empty;
        }

        return properties;
    }

    private static SyntaxColor ReadColor(XElement element, string? name)
        => new(name, (string?)element.Attribute("foreground"), (string?)element.Attribute("fontWeight"), (string?)element.Attribute("fontStyle"));

    // Couleur nommée (color="Comment") ou déclarée directement sur l'élément (foreground="#…") ; sans couleur, le texte
    // est reconnu mais garde la couleur par défaut.
    private static SyntaxColor? ResolveColor(XElement element, Dictionary<string, SyntaxColor> colors)
    {
        if ((string?)element.Attribute("color") is { Length: > 0 } name)
        {
            return colors.TryGetValue(name, out var color)
                ? color
                : throw new FormatException($"Couleur « {name} » non déclarée.");
        }

        return element.Attribute("foreground") is null && element.Attribute("fontWeight") is null && element.Attribute("fontStyle") is null
            ? null
            : ReadColor(element, null);
    }

    private static SyntaxRule ReadSpan(XElement element, Dictionary<string, SyntaxColor> colors, RegexOptions options)
    {
        var begin = (element.Element(Xshd + "Begin")?.Value ?? (string?)element.Attribute("begin"))?.Trim();
        if (string.IsNullOrEmpty(begin))
            throw new FormatException("Span sans début.");

        var end = (element.Element(Xshd + "End")?.Value ?? (string?)element.Attribute("end"))?.Trim();

        return new SyntaxRule(
            new Regex(begin, options),
            string.IsNullOrEmpty(end) ? null : new Regex(end, options),
            isSpan: true,
            isMultiline: (bool?)element.Attribute("multiline") == true,
            ResolveColor(element, colors));
    }

    private static SyntaxRule? ReadRule(XElement element, Dictionary<string, SyntaxColor> colors, RegexOptions options)
    {
        var pattern = element.Value.Trim();
        return pattern.Length == 0 ? null : new SyntaxRule(new Regex(pattern, options), null, false, false, ResolveColor(element, colors));
    }

    // Une seule expression par liste ; les mots les plus longs d'abord pour que « double » ne s'arrête pas à « do ».
    private static SyntaxRule? ReadKeywords(XElement element, Dictionary<string, SyntaxColor> colors, RegexOptions options, List<SyntaxKeyword> keywords)
    {
        var words = element.Elements(Xshd + "Word")
            .Select(w => w.Value.Trim())
            .Where(w => w.Length > 0)
            .ToArray();

        if (words.Length == 0)
            return null;

        var category = (string?)element.Attribute("color") ?? string.Empty;
        keywords.AddRange(words.Select(word => new SyntaxKeyword(word, category)));

        // Les \b évitent de colorer un mot-clé noyé dans un identifiant (SELECTED).
        var alternatives = words.Distinct(StringComparer.Ordinal).OrderByDescending(w => w.Length).Select(Regex.Escape);
        return new SyntaxRule(new Regex($@"\b(?>{string.Join('|', alternatives)})\b", options), null, false, false, ResolveColor(element, colors));
    }
}
