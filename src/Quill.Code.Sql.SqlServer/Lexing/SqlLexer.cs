using Quill.Code.Sql.Dialects;

namespace Quill.Code.Sql.SqlServer.Lexing;

/// <summary>
/// Minimal SQL lexer for completion. Unlike highlighting, it must tell comments, strings
/// and identifiers apart, and tolerate text being typed.
/// Delimiters, comments and parameters follow the dialect (T-SQL by default).
/// </summary>
public static class SqlLexer
{
    public static IReadOnlyList<SqlToken> Tokenize(string text, SqlDialect? dialect = null)
    {
        ArgumentNullException.ThrowIfNull(text);
        dialect ??= SqlServerDialect.Instance;

        List<SqlToken> tokens = [];
        var i = 0;

        while (i < text.Length)
        {
            var c = text[i];
            var next = i + 1 < text.Length ? text[i + 1] : '\0';
            var start = i;

            if (char.IsWhiteSpace(c))
            {
                i++;
                continue;
            }

            if ((c == '-' && next == '-' && StartsLineComment(text, i, dialect))
                || (c == '#' && dialect.HashLineComments))
            {
                i = text.IndexOf('\n', i);
                if (i < 0)
                    i = text.Length;

                // A line comment does not "close": caret at end of line = inside.
                tokens.Add(new SqlToken(SqlTokenKind.LineComment, start, TrimCarriageReturn(text, start, i) - start, false));
                continue;
            }

            if (c == '/' && next == '*')
            {
                var closed = SkipBlockComment(text, ref i, dialect.NestedBlockComments);
                tokens.Add(new SqlToken(SqlTokenKind.BlockComment, start, i - start, closed));
                continue;
            }

            if (c == '\'' || (c is 'N' or 'n' && next == '\'') || (c == '"' && dialect.DoubleQuotedStrings))
            {
                if (c is 'N' or 'n')
                    i++;

                var closed = SkipDelimited(text, ref i, c == '"' ? '"' : '\'', dialect.BackslashEscapesInStrings);
                tokens.Add(new SqlToken(SqlTokenKind.String, start, i - start, closed));
                continue;
            }

            if (c == '$' && dialect.DollarQuotedStrings && TrySkipDollarQuoted(text, ref i, out var isDollarClosed))
            {
                tokens.Add(new SqlToken(SqlTokenKind.String, start, i - start, isDollarClosed));
                continue;
            }

            if (FindIdentifierQuote(dialect, c) is { } quote)
            {
                var closed = SkipDelimited(text, ref i, quote.Close);
                tokens.Add(new SqlToken(SqlTokenKind.QuotedIdentifier, start, i - start, closed));
                continue;
            }

            if (c == '@' || IsColonParameter(text, i, dialect))
            {
                i++;
                while (i < text.Length && IsWordChar(text[i]))
                    i++;

                tokens.Add(new SqlToken(SqlTokenKind.Variable, start, i - start));
                continue;
            }

            if (char.IsDigit(c))
            {
                i = SkipNumber(text, i);
                tokens.Add(new SqlToken(SqlTokenKind.Number, start, i - start));
                continue;
            }

            if (IsWordStart(c))
            {
                i++;
                while (i < text.Length && IsWordChar(text[i]))
                    i++;

                tokens.Add(new SqlToken(SqlTokenKind.Word, start, i - start));
                continue;
            }

            var kind = c switch
            {
                '.' => SqlTokenKind.Dot,
                ',' => SqlTokenKind.Comma,
                '(' => SqlTokenKind.OpenParen,
                ')' => SqlTokenKind.CloseParen,
                ';' => SqlTokenKind.Semicolon,
                _ => SqlTokenKind.Operator
            };

            tokens.Add(new SqlToken(kind, start, 1));
            i++;
        }

        return tokens;
    }

    /// <summary>Leading # for temporary tables (#tmp, ##global).</summary>
    private static bool IsWordStart(char c) => char.IsLetter(c) || c is '_' or '#';

    private static bool IsWordChar(char c) => char.IsLetterOrDigit(c) || c is '_' or '@' or '#' or '$';

    private static int TrimCarriageReturn(string text, int start, int end)
        => end > start && text[end - 1] == '\r' ? end - 1 : end;

    private static bool StartsLineComment(string text, int index, SqlDialect dialect)
        => !dialect.DoubleDashRequiresWhitespace
            || index + 2 < text.Length && (char.IsWhiteSpace(text[index + 2]) || char.IsControl(text[index + 2]));

    private static SqlIdentifierQuote? FindIdentifierQuote(SqlDialect dialect, char c)
    {
        foreach (var quote in dialect.IdentifierQuotes)
        {
            if (quote.Open == c)
                return quote;
        }

        return null;
    }

    /// <summary>:name is a parameter; "::" (PostgreSQL cast) and ":=" are not.</summary>
    private static bool IsColonParameter(string text, int i, SqlDialect dialect)
        => dialect.ParameterPrefix == ':' && text[i] == ':'
            && i + 1 < text.Length && (char.IsLetter(text[i + 1]) || text[i + 1] == '_')
            && (i == 0 || text[i - 1] != ':');

    /// <summary>$$text$$ or $tag$text$tag$; false if the $ does not open a string ($1).</summary>
    private static bool TrySkipDollarQuoted(string text, ref int i, out bool isClosed)
    {
        isClosed = false;
        var tagEnd = i + 1;
        while (tagEnd < text.Length && (char.IsLetterOrDigit(text[tagEnd]) || text[tagEnd] == '_'))
            tagEnd++;

        if (tagEnd >= text.Length || text[tagEnd] != '$')
            return false;

        var tag = text[i..(tagEnd + 1)];
        var close = text.IndexOf(tag, tagEnd + 1, StringComparison.Ordinal);
        isClosed = close >= 0;
        i = isClosed ? close + tag.Length : text.Length;
        return true;
    }

    /// <summary>Block comments nest in T-SQL and PostgreSQL.</summary>
    private static bool SkipBlockComment(string text, ref int i, bool isNested)
    {
        var depth = 0;
        while (i < text.Length)
        {
            if (text[i] == '/' && i + 1 < text.Length && text[i + 1] == '*' && (isNested || depth == 0))
            {
                depth++;
                i += 2;
            }
            else if (text[i] == '*' && i + 1 < text.Length && text[i + 1] == '/')
            {
                depth--;
                i += 2;
                if (depth == 0)
                    return true;
            }
            else
            {
                i++;
            }
        }

        return false;
    }

    /// <summary>Doubled delimiter = escaped character ('' in a string, ]] in an identifier).</summary>
    private static bool SkipDelimited(string text, ref int i, char closing, bool backslashEscapes = false)
    {
        i++;
        while (i < text.Length)
        {
            if (backslashEscapes && text[i] == '\\' && i + 1 < text.Length)
            {
                i += 2;
                continue;
            }

            if (text[i] == closing)
            {
                if (i + 1 < text.Length && text[i + 1] == closing)
                {
                    i += 2;
                    continue;
                }

                i++;
                return true;
            }

            i++;
        }

        return false;
    }

    /// <summary>Integer, decimal, exponent or hexadecimal (0x1F): attached letters are part of the number.</summary>
    private static int SkipNumber(string text, int i)
    {
        while (i < text.Length)
        {
            var c = text[i];
            if (char.IsLetterOrDigit(c))
                i++;
            else if (c == '.' && i + 1 < text.Length && char.IsDigit(text[i + 1]))
                i++;
            else if (c is '+' or '-' && (text[i - 1] is 'e' or 'E'))
                i++;
            else
                break;
        }

        return i;
    }
}
