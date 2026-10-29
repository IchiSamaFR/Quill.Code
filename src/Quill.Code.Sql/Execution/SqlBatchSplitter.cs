using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Quill.Code.Sql.Execution;

/// <summary>
/// Decoupe un script sur les separateurs GO, comme SSMS : GO n'est pas du T-SQL,
/// le serveur le rejetterait. Un GO dans une chaine ou un commentaire bloc est ignore.
/// </summary>
public static partial class SqlBatchSplitter
{
    [GeneratedRegex(@"^\s*GO(?:\s+(?<count>\d+))?\s*(?:--.*)?$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex GoLineRegex();

    public static IReadOnlyList<SqlBatch> Split(string script)
    {
        ArgumentNullException.ThrowIfNull(script);

        var batches = new List<SqlBatch>();
        var current = new StringBuilder();
        var batchStartLine = 1;
        var lineNumber = 0;
        var state = new ScanState();

        foreach (var rawLine in script.Split('\n'))
        {
            lineNumber++;
            var line = rawLine.TrimEnd('\r');

            if (!state.IsInsideLiteral)
            {
                var match = GoLineRegex().Match(line);
                if (match.Success)
                {
                    var count = match.Groups["count"].Success
                        && int.TryParse(match.Groups["count"].Value, NumberStyles.None, CultureInfo.InvariantCulture, out var parsed)
                        && parsed > 0
                            ? parsed
                            : 1;

                    AddBatch(batches, current, batchStartLine, count);
                    current.Clear();
                    batchStartLine = lineNumber + 1;
                    continue;
                }
            }

            state.Scan(line);
            current.Append(line).Append('\n');
        }

        AddBatch(batches, current, batchStartLine, 1);
        return batches;
    }

    private static void AddBatch(List<SqlBatch> batches, StringBuilder text, int startLine, int repeatCount)
    {
        if (string.IsNullOrWhiteSpace(text.ToString()))
            return;

        batches.Add(new SqlBatch(text.ToString(), startLine, repeatCount));
    }

    /// <summary>Suit les chaines, identifiants [..] / ".." et commentaires bloc d'une ligne a l'autre.</summary>
    private sealed class ScanState
    {
        private int _blockCommentDepth;
        private char _closingQuote;

        public bool IsInsideLiteral => _blockCommentDepth > 0 || _closingQuote != '\0';

        public void Scan(string line)
        {
            for (var i = 0; i < line.Length; i++)
            {
                var c = line[i];
                var next = i + 1 < line.Length ? line[i + 1] : '\0';

                if (_blockCommentDepth > 0)
                {
                    if (c == '*' && next == '/')
                    {
                        _blockCommentDepth--;
                        i++;
                    }
                    else if (c == '/' && next == '*')
                    {
                        _blockCommentDepth++;
                        i++;
                    }
                    continue;
                }

                if (_closingQuote != '\0')
                {
                    if (c == _closingQuote)
                    {
                        // Quote doublee = caractere echappe, on reste dans le litteral.
                        if (next == _closingQuote)
                            i++;
                        else
                            _closingQuote = '\0';
                    }
                    continue;
                }

                switch (c)
                {
                    case '-' when next == '-':
                        return;
                    case '/' when next == '*':
                        _blockCommentDepth++;
                        i++;
                        break;
                    case '\'':
                    case '"':
                        _closingQuote = c;
                        break;
                    case '[':
                        _closingQuote = ']';
                        break;
                }
            }
        }
    }
}
