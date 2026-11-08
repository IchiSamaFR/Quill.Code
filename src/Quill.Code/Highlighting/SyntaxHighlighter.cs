using System.Text.RegularExpressions;

namespace Quill.Code.Highlighting;

/// <summary>
/// Splits a text into colored spans according to a <see cref="SyntaxDefinition"/>. Works line by line:
/// only the "open multi-line span" state (<see cref="SyntaxLineState"/>) carries from one line to the next.
/// </summary>
/// <remarks>
/// At each position, the nearest match wins; on a tie, the rule declared first
/// (like AvalonEdit). Returned spans are ordered and disjoint.
/// </remarks>
public static class SyntaxHighlighter
{
    /// <summary>
    /// Colors one line (without its line break); <paramref name="state"/> is the end state of the previous line
    /// (<see cref="SyntaxLineState.Initial"/> for the first one).
    /// </summary>
    public static IReadOnlyList<SyntaxToken> HighlightLine(SyntaxDefinition definition, string line, SyntaxLineState state, out SyntaxLineState endState)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(line);

        var rules = definition.Rules;
        List<SyntaxToken> tokens = [];
        endState = SyntaxLineState.Initial;
        var position = 0;

        // Suite d'un span ouvert sur une ligne précédente.
        if (state.OpenSpan >= 0 && state.OpenSpan < rules.Count)
        {
            var openSpan = rules[state.OpenSpan];
            var spanEnd = FindSpanEnd(openSpan, line, 0);
            if (spanEnd < 0)
            {
                Add(tokens, 0, line.Length, openSpan.Color);
                endState = state;
                return tokens;
            }

            Add(tokens, 0, spanEnd, openSpan.Color);
            position = spanEnd;
        }

        // Prochaine correspondance de chaque règle : recalculée seulement quand on l'a dépassée.
        var nextMatches = new Match?[rules.Count];

        while (position < line.Length)
        {
            var bestRule = -1;
            Match? best = null;

            for (var i = 0; i < rules.Count; i++)
            {
                var match = nextMatches[i];
                if (match is null || (match.Success && match.Index < position))
                {
                    match = FindNonEmptyMatch(rules[i].Begin, line, position);
                    nextMatches[i] = match;
                }

                // Strictement plus tôt : à égalité, la règle déclarée en premier l'emporte.
                if (match.Success && (best is null || match.Index < best.Index))
                {
                    best = match;
                    bestRule = i;
                }
            }

            if (best is null)
                break;

            var rule = rules[bestRule];
            var end = best.Index + best.Length;

            if (rule.IsSpan)
            {
                var spanEnd = FindSpanEnd(rule, line, end);
                if (spanEnd < 0)
                {
                    // Span sans fin sur cette ligne : jusqu'au bout, et au-delà s'il est multiligne.
                    Add(tokens, best.Index, line.Length - best.Index, rule.Color);
                    if (rule.IsMultiline && rule.End is not null)
                        endState = new SyntaxLineState(bestRule);

                    return tokens;
                }

                end = spanEnd;
            }

            Add(tokens, best.Index, end - best.Index, rule.Color);
            position = end;
        }

        return tokens;
    }

    /// <summary>
    /// Colors a whole text (\n or \r\n line breaks); positions in <paramref name="text"/>.
    /// A multi-line span yields one span per line, line breaks staying outside the spans.
    /// </summary>
    public static IReadOnlyList<SyntaxToken> Highlight(SyntaxDefinition definition, string text, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(text);

        List<SyntaxToken> tokens = [];
        var state = SyntaxLineState.Initial;
        var lineStart = 0;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var newLine = text.IndexOf('\n', lineStart);
            var lineEnd = newLine < 0 ? text.Length : newLine;
            if (lineEnd > lineStart && text[lineEnd - 1] == '\r')
                lineEnd--;

            foreach (var token in HighlightLine(definition, text[lineStart..lineEnd], state, out state))
                tokens.Add(token with { Start = token.Start + lineStart });

            if (newLine < 0)
                return tokens;

            lineStart = newLine + 1;
        }
    }

    private static void Add(List<SyntaxToken> tokens, int start, int length, SyntaxColor? color)
    {
        if (length > 0)
            tokens.Add(new SyntaxToken(start, length, color));
    }

    private static int FindSpanEnd(SyntaxRule span, string line, int start)
    {
        if (span.End is null)
            return -1;

        var match = span.End.Match(line, start);
        return match.Success ? match.Index + match.Length : -1;
    }

    // Une correspondance vide ne colore rien et bloquerait l'avancée : on cherche plus loin.
    private static Match FindNonEmptyMatch(Regex regex, string line, int start)
    {
        var match = regex.Match(line, start);
        while (match.Success && match.Length == 0)
        {
            if (match.Index >= line.Length)
                return Match.Empty;

            match = regex.Match(line, match.Index + 1);
        }

        return match;
    }
}
