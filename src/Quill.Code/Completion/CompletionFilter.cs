namespace Quill.Code.Completion;

/// <summary>Filtre et tri des propositions sur le mot en cours de frappe, rejoué à chaque touche sans relancer le fournisseur.</summary>
public static class CompletionFilter
{
    /// <summary>Au-delà, la liste n'aide plus : mieux vaut taper une lettre de plus.</summary>
    public const int DefaultMaxResults = 100;

    /// <summary>
    /// Propositions dont <see cref="CompletionItem.TextToFilter"/> contient <paramref name="prefix"/> (sans casse) :
    /// celles qui commencent par lui d'abord, puis celles où il ouvre un segment (NUM → ROW_NUMBER), puis les autres ;
    /// à égalité, par nature puis par libellé.
    /// </summary>
    public static IReadOnlyList<CompletionItem> Filter(IReadOnlyList<CompletionItem> items, string? prefix, int maxResults = DefaultMaxResults)
    {
        ArgumentNullException.ThrowIfNull(items);
        prefix ??= string.Empty;

        // Une passe, un tri en place, sans LINQ : appelé sur le thread de l'interface à chaque frappe.
        var matches = new List<(CompletionItem Item, int Score)>(Math.Min(items.Count, 512));
        for (var i = 0; i < items.Count; i++)
        {
            var score = Score(items[i].TextToFilter, prefix);
            if (score >= 0)
                matches.Add((items[i], score));
        }

        matches.Sort(static (a, b) =>
        {
            var result = a.Score.CompareTo(b.Score);
            if (result == 0)
                result = a.Item.Kind.CompareTo(b.Item.Kind);
            return result != 0 ? result : StringComparer.OrdinalIgnoreCase.Compare(a.Item.Label, b.Item.Label);
        });

        var count = Math.Min(matches.Count, maxResults);
        var filtered = new CompletionItem[count];
        for (var i = 0; i < count; i++)
            filtered[i] = matches[i].Item;

        return filtered;
    }

    /// <summary>0 = commence par, 1 = début d'un segment, 2 = contient, -1 = rejeté.</summary>
    private static int Score(string text, string prefix)
    {
        if (prefix.Length == 0 || text.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return 0;

        var index = text.IndexOf(prefix, StringComparison.OrdinalIgnoreCase);
        if (index < 0)
            return -1;

        return text[index - 1] == '_' ? 1 : 2;
    }
}
