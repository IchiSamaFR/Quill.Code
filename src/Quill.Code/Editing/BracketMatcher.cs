namespace Quill.Code.Editing;

/// <summary>Crochet associé à celui qui touche le curseur ; ceux des chaînes et des commentaires sont ignorés.</summary>
public static class BracketMatcher
{
    private const string Openers = "([{";
    private const string Closers = ")]}";

    /// <summary>
    /// Paire dont un crochet touche le curseur : comme Visual Studio, fermant juste avant le curseur, puis ouvrant
    /// juste après. <paramref name="regions"/> vient de <see cref="SourceScanner.Classify"/> sur le même texte.
    /// </summary>
    public static BracketPair? FindPair(string text, int caret, IReadOnlyList<SourceRegion> regions)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(regions);

        foreach (var index in (ReadOnlySpan<int>)[caret - 1, caret])
        {
            if (index < 0 || index >= text.Length || index >= regions.Count || regions[index] != SourceRegion.Code)
                continue;

            var match = FindMatch(text, index, regions);
            if (match >= 0)
                return new BracketPair(Math.Min(index, match), Math.Max(index, match));
        }

        return null;
    }

    /// <inheritdoc cref="FindPair(string, int, IReadOnlyList{SourceRegion})"/>
    public static BracketPair? FindPair(string text, int caret, SourceSyntax syntax)
        => FindPair(text, caret, SourceScanner.Classify(text, syntax));

    /// <summary>Position du crochet associé à celui de <paramref name="index"/>, -1 si aucun.</summary>
    public static int FindMatch(string text, int index, IReadOnlyList<SourceRegion> regions)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(regions);

        if (index < 0 || index >= text.Length)
            return -1;

        var bracket = text[index];
        var opener = Openers.IndexOf(bracket, StringComparison.Ordinal);
        var closer = Closers.IndexOf(bracket, StringComparison.Ordinal);
        if (opener < 0 && closer < 0)
            return -1;

        var forward = opener >= 0;
        var kind = forward ? opener : closer;
        var open = Openers[kind];
        var close = Closers[kind];
        var end = Math.Min(text.Length, regions.Count);

        var depth = 0;
        for (var i = index; forward ? i < end : i >= 0; i += forward ? 1 : -1)
        {
            if (regions[i] != SourceRegion.Code)
                continue;

            if (text[i] == open)
                depth += forward ? 1 : -1;
            else if (text[i] == close)
                depth += forward ? -1 : 1;

            if (depth == 0)
                return i;
        }

        return -1;
    }
}
