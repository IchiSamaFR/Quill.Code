using Quill.Code.Constants;

namespace Quill.Code.Completion;

/// <summary>Filters and sorts suggestions on the word being typed, replayed on each keystroke without calling the provider again.</summary>
public static class CompletionFilter
{
    /// <summary>
    /// Suggestions whose <see cref="CompletionItem.TextToFilter"/> contains <paramref name="prefix"/> (case-insensitive):
    /// those starting with it first, then those where it starts a segment (NUM → ROW_NUMBER), then the others;
    /// ties broken by kind, then by label.
    /// </summary>
    public static IReadOnlyList<CompletionItem> Filter(IReadOnlyList<CompletionItem> items, string? prefix, int maxResults = CompletionConstants.DefaultMaxResults)
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

    /// <summary>0 = starts with, 1 = start of a segment, 2 = contains, -1 = rejected.</summary>
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
