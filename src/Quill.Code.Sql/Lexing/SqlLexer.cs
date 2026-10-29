namespace Quill.Code.Sql.Lexing;

/// <summary>
/// Lexer T-SQL minimal pour l'autocompletion. Contrairement a la coloration, il doit
/// distinguer commentaires, chaines et identifiants, et tolerer un texte en cours de frappe.
/// </summary>
public static class SqlLexer
{
    public static IReadOnlyList<SqlToken> Tokenize(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

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

            if (c == '-' && next == '-')
            {
                i = text.IndexOf('\n', i);
                if (i < 0)
                    i = text.Length;

                // Une ligne de commentaire ne se "referme" pas : curseur en fin de ligne = dedans.
                tokens.Add(new SqlToken(SqlTokenKind.LineComment, start, TrimCarriageReturn(text, start, i) - start, false));
                continue;
            }

            if (c == '/' && next == '*')
            {
                var closed = SkipBlockComment(text, ref i);
                tokens.Add(new SqlToken(SqlTokenKind.BlockComment, start, i - start, closed));
                continue;
            }

            if (c == '\'' || (c is 'N' or 'n' && next == '\''))
            {
                if (c != '\'')
                    i++;

                var closed = SkipDelimited(text, ref i, '\'');
                tokens.Add(new SqlToken(SqlTokenKind.String, start, i - start, closed));
                continue;
            }

            if (c is '[' or '"')
            {
                var closed = SkipDelimited(text, ref i, c == '[' ? ']' : '"');
                tokens.Add(new SqlToken(SqlTokenKind.QuotedIdentifier, start, i - start, closed));
                continue;
            }

            if (c == '@')
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

    /// <summary># en tete pour les tables temporaires (#tmp, ##global).</summary>
    private static bool IsWordStart(char c) => char.IsLetter(c) || c is '_' or '#';

    private static bool IsWordChar(char c) => char.IsLetterOrDigit(c) || c is '_' or '@' or '#' or '$';

    private static int TrimCarriageReturn(string text, int start, int end)
        => end > start && text[end - 1] == '\r' ? end - 1 : end;

    /// <summary>Les commentaires bloc s'imbriquent en T-SQL.</summary>
    private static bool SkipBlockComment(string text, ref int i)
    {
        var depth = 0;
        while (i < text.Length)
        {
            if (text[i] == '/' && i + 1 < text.Length && text[i + 1] == '*')
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

    /// <summary>Delimiteur double = caractere echappe ('' dans une chaine, ]] dans un identifiant).</summary>
    private static bool SkipDelimited(string text, ref int i, char closing)
    {
        i++;
        while (i < text.Length)
        {
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

    /// <summary>Entier, decimal, exposant ou hexadecimal (0x1F) : les lettres collees font partie du nombre.</summary>
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
