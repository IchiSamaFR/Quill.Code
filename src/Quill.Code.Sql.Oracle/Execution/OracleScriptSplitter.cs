using Quill.Code.Execution;
using Quill.Code.Sql.Oracle.Constants;

namespace Quill.Code.Sql.Oracle.Execution;

/// <summary>
/// Decoupage d'un script Oracle a la maniere de SQL*Plus : une instruction SQL se termine par « ; » (retire : Oracle le refuse),
/// un bloc PL/SQL (DECLARE, BEGIN, CREATE PROCEDURE...) par une ligne « / ». Chaines, identifiants entre guillemets,
/// q'[...]' et commentaires sont respectes.
/// </summary>
internal static class OracleScriptSplitter
{
    public static IReadOnlyList<QueryBatch> Split(string script)
    {
        ArgumentNullException.ThrowIfNull(script);

        var batches = new List<QueryBatch>();
        var line = 1;
        var position = 0;
        while (true)
        {
            position = SkipTrivia(script, position, ref line);
            if (position >= script.Length)
                break;

            var startLine = line;
            var end = FindEnd(script, position, IsBlock(script, position), ref line, out var next);
            var text = script[position..end].TrimEnd();
            if (text.Length > 0)
                batches.Add(new QueryBatch(text, startLine));

            position = next;
        }

        return batches;
    }

    /// <summary>Fin de l'instruction commencee en <paramref name="start"/> (terminateur exclu) ; <paramref name="next"/> : suite du script.</summary>
    private static int FindEnd(string s, int start, bool isBlock, ref int line, out int next)
    {
        var i = start;
        while (i < s.Length)
        {
            var c = s[i];
            if (c == '/' && IsSlashLine(s, i, requireLineStart: true, out var lineEnd))
            {
                CountLineBreak(s, lineEnd, ref line);
                next = lineEnd;
                return i;
            }

            switch (c)
            {
                case '\n':
                    line++;
                    i++;
                    break;
                case '\'' or '"':
                    i = SkipQuoted(s, i, c, ref line);
                    break;
                case 'q' or 'Q' when IsQQuoteStart(s, i):
                    i = SkipQQuote(s, i, ref line);
                    break;
                case '-' when At(s, i + 1) == '-':
                    i = SkipLineComment(s, i);
                    break;
                case '/' when At(s, i + 1) == '*':
                    i = SkipBlockComment(s, i, ref line);
                    break;
                case ';' when !isBlock:
                    next = i + 1;
                    return i;
                default:
                    i++;
                    break;
            }
        }

        next = s.Length;
        return s.Length;
    }

    /// <summary>Saute blancs, commentaires et lignes « / » isolees entre deux instructions.</summary>
    private static int SkipTrivia(string s, int i, ref int line)
    {
        while (i < s.Length)
        {
            var c = s[i];
            if (char.IsWhiteSpace(c))
            {
                if (c == '\n')
                    line++;
                i++;
            }
            else if (c == '-' && At(s, i + 1) == '-')
            {
                i = SkipLineComment(s, i);
            }
            else if (c == '/' && At(s, i + 1) == '*')
            {
                i = SkipBlockComment(s, i, ref line);
            }
            else if (c == '/' && IsSlashLine(s, i, requireLineStart: false, out var lineEnd))
            {
                CountLineBreak(s, lineEnd, ref line);
                i = lineEnd;
            }
            else
            {
                return i;
            }
        }

        return i;
    }

    /// <summary>Vrai si l'instruction est un bloc PL/SQL (DECLARE, BEGIN, CREATE [OR REPLACE] PROCEDURE...).</summary>
    private static bool IsBlock(string s, int start)
    {
        var words = ReadLeadingWords(s, start);
        if (words.Count == 0)
            return false;

        if (OracleScriptConstants.AnonymousBlockKeywords.Contains(words[0]))
            return true;

        if (!string.Equals(words[0], OracleScriptConstants.CreateKeyword, StringComparison.OrdinalIgnoreCase))
            return false;

        foreach (var word in words.Skip(1))
        {
            if (!OracleScriptConstants.CreateModifiers.Contains(word))
                return OracleScriptConstants.BlockObjectTypes.Contains(word);
        }

        return false;
    }

    private static List<string> ReadLeadingWords(string s, int i)
    {
        var words = new List<string>(OracleScriptConstants.MaxLeadingWords);
        while (words.Count < OracleScriptConstants.MaxLeadingWords)
        {
            while (i < s.Length && char.IsWhiteSpace(s[i]))
                i++;

            var start = i;
            while (i < s.Length && IsWordChar(s[i]))
                i++;

            if (i == start)
                break;

            words.Add(s[start..i]);
        }

        return words;
    }

    /// <summary>Ligne faite d'un seul « / » (blancs autorises) ; <paramref name="lineEnd"/> : debut de la ligne suivante.</summary>
    private static bool IsSlashLine(string s, int slash, bool requireLineStart, out int lineEnd)
    {
        lineEnd = slash;
        if (requireLineStart)
        {
            for (var j = slash - 1; j >= 0 && s[j] != '\n'; j--)
            {
                if (s[j] is not (' ' or '\t' or '\r'))
                    return false;
            }
        }

        var k = slash + 1;
        while (k < s.Length && s[k] is ' ' or '\t' or '\r')
            k++;

        if (k < s.Length && s[k] != '\n')
            return false;

        lineEnd = k < s.Length ? k + 1 : k;
        return true;
    }

    private static void CountLineBreak(string s, int lineEnd, ref int line)
    {
        if (lineEnd > 0 && s[lineEnd - 1] == '\n')
            line++;
    }

    private static int SkipQuoted(string s, int i, char quote, ref int line)
    {
        var j = i + 1;
        while (j < s.Length)
        {
            if (s[j] == quote)
            {
                // Guillemet double : echappement, la chaine continue.
                if (At(s, j + 1) == quote)
                {
                    j += 2;
                    continue;
                }

                return j + 1;
            }

            if (s[j] == '\n')
                line++;
            j++;
        }

        return s.Length;
    }

    /// <summary>q'X...X' (ou Nq'...') : chaine dont le delimiteur est choisi, sans echappement.</summary>
    private static bool IsQQuoteStart(string s, int i)
    {
        if (At(s, i + 1) != '\'' || i + 2 >= s.Length)
            return false;

        if (i == 0 || !IsWordChar(s[i - 1]))
            return true;

        return s[i - 1] is 'n' or 'N' && (i < 2 || !IsWordChar(s[i - 2]));
    }

    private static int SkipQQuote(string s, int i, ref int line)
    {
        var close = s[i + 2] switch
        {
            '[' => ']',
            '(' => ')',
            '{' => '}',
            '<' => '>',
            var other => other,
        };

        for (var j = i + 3; j < s.Length; j++)
        {
            if (s[j] == close && At(s, j + 1) == '\'')
                return j + 2;
            if (s[j] == '\n')
                line++;
        }

        return s.Length;
    }

    private static int SkipLineComment(string s, int i)
    {
        var j = i + 2;
        while (j < s.Length && s[j] != '\n')
            j++;
        return j;
    }

    private static int SkipBlockComment(string s, int i, ref int line)
    {
        var j = i + 2;
        while (j + 1 < s.Length && !(s[j] == '*' && s[j + 1] == '/'))
        {
            if (s[j] == '\n')
                line++;
            j++;
        }

        return Math.Min(j + 2, s.Length);
    }

    private static char At(string s, int i) => i < s.Length ? s[i] : '\0';

    private static bool IsWordChar(char c) => char.IsLetterOrDigit(c) || c is '_' or '$' or '#';
}
