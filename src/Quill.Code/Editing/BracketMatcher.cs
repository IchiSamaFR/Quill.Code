using Quill.Code.Constants;

namespace Quill.Code.Editing;

/// <summary>Bracket matching the one touching the caret; those in strings and comments are ignored.</summary>
public static class BracketMatcher
{
    /// <summary>
    /// Pair with a bracket touching the caret: like Visual Studio, closing bracket just before the caret, then opening
    /// bracket just after. <paramref name="regions"/> comes from <see cref="SourceScanner.Classify"/> on the same text.
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

    /// <summary>Position of the bracket matching the one at <paramref name="index"/>, -1 if none.</summary>
    public static int FindMatch(string text, int index, IReadOnlyList<SourceRegion> regions)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(regions);

        if (index < 0 || index >= text.Length)
            return -1;

        var bracket = text[index];
        var opener = EditingConstants.BracketOpeners.IndexOf(bracket, StringComparison.Ordinal);
        var closer = EditingConstants.BracketClosers.IndexOf(bracket, StringComparison.Ordinal);
        if (opener < 0 && closer < 0)
            return -1;

        var forward = opener >= 0;
        var kind = forward ? opener : closer;
        var open = EditingConstants.BracketOpeners[kind];
        var close = EditingConstants.BracketClosers[kind];
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
