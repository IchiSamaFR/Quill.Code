using Quill.Code.Constants;

namespace Quill.Code.Editing;

/// <summary>
/// Minimal splitting of the source into code, comments and strings (no highlighting), according to a <see cref="SourceSyntax"/>.
/// </summary>
public static class SourceScanner
{
    /// <summary>Region of each character of the text (to cache as long as the text does not change).</summary>
    public static SourceRegion[] Classify(string text, SourceSyntax syntax)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(syntax);

        var regions = new SourceRegion[text.Length];
        Scan(text, text.Length, syntax, regions);
        return regions;
    }

    /// <summary>Region an insertion at <paramref name="offset"/> falls into.</summary>
    public static SourceRegion RegionAt(string text, int offset, SourceSyntax syntax)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(syntax);

        return Scan(text, Math.Clamp(offset, 0, text.Length), syntax, null);
    }

    /// <returns>The region still open after text[..limit].</returns>
    private static SourceRegion Scan(string text, int limit, SourceSyntax syntax, SourceRegion[]? regions)
    {
        var state = SourceRegion.Code;
        var quote = '\0';
        var isVerbatim = false;
        var rawLength = 0;

        var i = 0;
        while (i < limit)
        {
            var c = text[i];
            var next = i + 1 < limit ? text[i + 1] : '\0';

            // Region of the consumed characters: a closing delimiter still belongs to its region.
            var region = state;
            var length = 1;

            switch (state)
            {
                case SourceRegion.Code:
                    if (StartsWith(text, i, limit, syntax.LineComment))
                    {
                        state = region = SourceRegion.LineComment;
                        length = syntax.LineComment!.Length;
                    }
                    else if (StartsWith(text, i, limit, syntax.BlockCommentStart))
                    {
                        state = region = SourceRegion.BlockComment;
                        length = syntax.BlockCommentStart!.Length;
                    }
                    else if (c == '"' && syntax.RawStrings && StartsWith(text, i, limit, EditingConstants.RawStringDelimiter))
                    {
                        state = region = SourceRegion.String;
                        rawLength = CountQuotes(text, i, limit);
                        length = rawLength;
                    }
                    else if (c is '\'' or '"')
                    {
                        state = region = SourceRegion.String;
                        quote = c;
                        rawLength = 0;
                        isVerbatim = c == '"' && syntax.VerbatimStrings && IsVerbatimPrefix(text, i);
                    }
                    break;

                case SourceRegion.LineComment:
                    if (c == '\n')
                        state = region = SourceRegion.Code;
                    break;

                case SourceRegion.BlockComment:
                    if (StartsWith(text, i, limit, syntax.BlockCommentEnd))
                    {
                        length = syntax.BlockCommentEnd!.Length;
                        state = SourceRegion.Code;
                    }
                    break;

                case SourceRegion.String when rawLength > 0:
                    if (c == '"' && CountQuotes(text, i, limit) >= rawLength)
                    {
                        length = rawLength;
                        state = SourceRegion.Code;
                    }
                    break;

                case SourceRegion.String:
                    if (c == quote)
                    {
                        if ((syntax.DoubledQuoteEscapes || isVerbatim) && next == quote)
                            length = 2;
                        else
                            state = SourceRegion.Code;
                    }
                    else if (c == '\\' && syntax.BackslashEscapes && !isVerbatim)
                    {
                        length = 2;
                    }
                    else if (c == '\n' && syntax.StringsEndAtLineEnd && !isVerbatim)
                    {
                        // Unterminated string: it stops at the line.
                        state = region = SourceRegion.Code;
                    }
                    break;
            }

            length = Math.Min(length, limit - i);
            regions?.AsSpan(i, length).Fill(region);
            i += length;
        }

        return state;
    }

    private static bool StartsWith(string text, int index, int limit, string? token)
        => !string.IsNullOrEmpty(token) && index + token.Length <= limit && string.CompareOrdinal(text, index, token, 0, token.Length) == 0;

    private static int CountQuotes(string text, int index, int limit)
    {
        var end = index;
        while (end < limit && text[end] == '"')
            end++;

        return end - index;
    }

    // @"...", $@"..." or @$"...".
    private static bool IsVerbatimPrefix(string text, int quoteIndex)
        => quoteIndex > 0 && (text[quoteIndex - 1] == '@'
            || (quoteIndex > 1 && text[quoteIndex - 1] == '$' && text[quoteIndex - 2] == '@'));
}
