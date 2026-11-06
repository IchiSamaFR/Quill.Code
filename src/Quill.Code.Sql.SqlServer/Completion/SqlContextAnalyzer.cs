using Quill.Code.Sql.SqlServer.Lexing;
using System.Text.RegularExpressions;

namespace Quill.Code.Sql.SqlServer.Completion;

/// <summary>
/// Devine ce que le T-SQL attend au curseur, sans vrai parser : jeton precedent d'abord,
/// puis mot-cle de clause le plus proche au meme niveau de parentheses. L'analyse se limite
/// au lot courant (delimite par GO), comme l'execution.
/// </summary>
public static partial class SqlContextAnalyzer
{
    /// <summary>Mots apres lesquels on attend un nom de table.</summary>
    private static readonly HashSet<string> TableSourceKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "FROM", "JOIN", "UPDATE", "INTO", "TABLE", "MERGE", "USING", "APPLY"
    };

    /// <summary>Sources ou un alias peut suivre le nom de la table.</summary>
    private static readonly HashSet<string> AliasedSources = new(StringComparer.OrdinalIgnoreCase)
    {
        "FROM", "JOIN", "MERGE", "USING", "APPLY"
    };

    /// <summary>Clauses dont le contenu est une expression (colonnes, fonctions).</summary>
    private static readonly HashSet<string> ExpressionClauses = new(StringComparer.OrdinalIgnoreCase)
    {
        "SELECT", "WHERE", "ON", "HAVING", "BY", "SET", "VALUES", "OUTPUT", "IF", "WHILE", "RETURN", "PRINT"
    };

    /// <summary>Clauses qui ouvrent une nouvelle instruction.</summary>
    private static readonly HashSet<string> StatementClauses = new(StringComparer.OrdinalIgnoreCase)
    {
        "UNION", "EXCEPT", "INTERSECT", "BEGIN", "END", "DECLARE", "EXEC", "EXECUTE"
    };

    /// <summary>Mots qui ne peuvent pas etre un nom ou un alias de table.</summary>
    private static readonly HashSet<string> ReservedWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "SELECT", "FROM", "WHERE", "GROUP", "ORDER", "BY", "HAVING", "JOIN", "INNER", "LEFT", "RIGHT", "FULL",
        "OUTER", "CROSS", "APPLY", "ON", "AS", "UNION", "EXCEPT", "INTERSECT", "SET", "VALUES", "OUTPUT", "INTO",
        "WITH", "OPTION", "WHEN", "THEN", "ELSE", "END", "AND", "OR", "NOT", "INSERT", "UPDATE", "DELETE", "MERGE",
        "USING", "PIVOT", "UNPIVOT", "FOR", "GO", "USE", "DECLARE", "BEGIN", "IF", "WHILE", "RETURN", "TABLE",
        "EXEC", "EXECUTE", "TOP", "DISTINCT", "ALL", "CASE", "OFFSET", "FETCH", "WINDOW", "TABLESAMPLE"
    };

    private static readonly HashSet<string> CastFunctions = new(StringComparer.OrdinalIgnoreCase) { "CAST", "TRY_CAST" };

    private static readonly HashSet<string> ConvertFunctions = new(StringComparer.OrdinalIgnoreCase) { "CONVERT", "TRY_CONVERT" };

    /// <summary>Reste de la ligne apres GO : un nombre de repetitions et un commentaire, rien d'autre.</summary>
    [GeneratedRegex(@"^[ \t]*\d*[ \t]*(?:--.*)?\r?$", RegexOptions.CultureInvariant)]
    private static partial Regex GoLineTailRegex();

    /// <summary>Analyse le texte autour de <paramref name="caret"/>.</summary>
    /// <param name="text">Texte complet de l'éditeur.</param>
    /// <param name="caret">Position du curseur.</param>
    /// <param name="database">Base imposée par l'appelant (dashboard…), si le texte n'en choisit pas par USE.</param>
    public static SqlContext Analyze(string text, int caret, string? database = null)
    {
        ArgumentNullException.ThrowIfNull(text);

        caret = Math.Clamp(caret, 0, text.Length);
        var wordStart = FindWordStart(text, caret);
        var all = SqlLexer.Tokenize(text);

        if (all.Any(token => IsCaretInside(token, caret)) || (wordStart < caret && text[wordStart] == '@'))
            return SqlContext.None;

        var (batchStart, batchEnd) = FindBatch(text, all, caret);
        var tokens = all.Where(t => !t.IsComment && t.Start >= batchStart && t.End <= batchEnd).ToArray();

        // Jetons strictement avant le mot en cours de frappe.
        var index = tokens.Count(t => t.End <= wordStart);

        var (kind, qualifier) = DetermineKind(text, tokens, index, wordStart);
        if (kind == SqlContextKind.None)
            return SqlContext.None;

        // Le premier mot du qualificatif est analyse comme s'il etait en cours de frappe.
        var parentKind = SqlContextKind.General;
        if (kind == SqlContextKind.MemberAccess)
        {
            var first = index - (2 * qualifier.Count);
            parentKind = DetermineKind(text, tokens, first, tokens[first].Start).Kind;
        }

        return new SqlContext
        {
            Kind = kind,
            Qualifier = qualifier,
            MemberParentKind = parentKind,
            Tables = ParseTables(text, tokens).OrderBy(t => Math.Abs(t.Offset - caret)).ToArray(),
            // Un USE du texte l'emporte sur la base imposee par l'appelant (dashboard).
            Database = FindDatabase(text, all, caret) ?? (string.IsNullOrWhiteSpace(database) ? null : database)
        };
    }

    /// <summary>
    /// Tables et base du lot contenant <paramref name="offset"/>, sans deviner ce qui est attendu :
    /// l'infobulle en a besoin sur un nom deja ecrit, la ou <see cref="Analyze"/> ne propose rien.
    /// </summary>
    public static SqlContext AnalyzeScope(string text, int offset, string? database)
    {
        ArgumentNullException.ThrowIfNull(text);

        var all = SqlLexer.Tokenize(text);
        var (batchStart, batchEnd) = FindBatch(text, all, offset);
        var tokens = all.Where(t => !t.IsComment && t.Start >= batchStart && t.End <= batchEnd).ToArray();

        return new SqlContext
        {
            Kind = SqlContextKind.General,
            Tables = ParseTables(text, tokens).OrderBy(t => Math.Abs(t.Offset - offset)).ToArray(),
            Database = FindDatabase(text, all, offset) ?? (string.IsNullOrWhiteSpace(database) ? null : database)
        };
    }

    /// <summary>Début du mot en cours de frappe (@ et # pour les variables et tables temporaires).</summary>
    public static int FindWordStart(string text, int caret)
    {
        var start = Math.Clamp(caret, 0, text.Length);
        while (start > 0 && IsIdentifierChar(text[start - 1]))
            start--;

        return start;
    }

    private static bool IsIdentifierChar(char c) => char.IsLetterOrDigit(c) || c is '_' or '@' or '#' or '$';

    // ---------- Position du curseur ----------

    private static bool IsCaretInside(SqlToken token, int caret)
    {
        if (token.Kind is not (SqlTokenKind.String or SqlTokenKind.QuotedIdentifier or SqlTokenKind.LineComment or SqlTokenKind.BlockComment))
            return false;

        return token.Start < caret && (caret < token.End || (caret == token.End && !token.IsTerminated));
    }

    /// <summary>Bornes du lot contenant le curseur : entre les lignes GO qui l'encadrent.</summary>
    private static (int Start, int End) FindBatch(string text, IReadOnlyList<SqlToken> tokens, int caret)
    {
        var start = 0;
        var end = text.Length;

        foreach (var token in tokens.Where(t => t.IsWord(text, "GO") && IsGoLine(text, t)))
        {
            if (token.End <= caret)
                start = token.End;
            else if (token.Start >= caret)
            {
                end = token.Start;
                break;
            }
        }

        return (start, end);
    }

    private static bool IsGoLine(string text, SqlToken token)
    {
        var lineStart = token.Start == 0 ? 0 : text.LastIndexOf('\n', token.Start - 1) + 1;
        if (!string.IsNullOrWhiteSpace(text[lineStart..token.Start]))
            return false;

        var lineEnd = text.IndexOf('\n', token.End);
        return GoLineTailRegex().IsMatch(text[token.End..(lineEnd < 0 ? text.Length : lineEnd)]);
    }

    // ---------- Nature attendue ----------

    private static (SqlContextKind Kind, IReadOnlyList<string> Qualifier) DetermineKind(string text, SqlToken[] tokens, int index, int wordStart)
    {
        if (index == 0)
            return (SqlContextKind.General, []);

        var previous = tokens[index - 1];

        if (previous.Kind == SqlTokenKind.Dot)
        {
            var qualifier = ReadQualifier(text, tokens, index - 1);
            if (qualifier.Count == 0)
                return (SqlContextKind.None, []);

            return (SqlContextKind.MemberAccess, qualifier);
        }

        if (previous.Kind == SqlTokenKind.Word)
        {
            var word = previous.GetText(text);

            if (word.Equals("USE", StringComparison.OrdinalIgnoreCase))
                return (SqlContextKind.Database, []);

            if (TableSourceKeywords.Contains(word))
                return (SqlContextKind.TableSource, []);

            if (word.Equals("EXEC", StringComparison.OrdinalIgnoreCase) || word.Equals("EXECUTE", StringComparison.OrdinalIgnoreCase))
                return (SqlContextKind.Procedure, []);

            if (word.Equals("AS", StringComparison.OrdinalIgnoreCase))
                return (AfterAs(text, tokens, index - 1), []);
        }

        // CONVERT(type, valeur) : le type vient en premier.
        if (previous.Kind == SqlTokenKind.OpenParen && index >= 2 && IsWordIn(text, tokens[index - 2], ConvertFunctions))
            return (SqlContextKind.DataType, []);

        var (clause, isInsideParen) = FindClause(text, tokens, index);

        if (previous.Kind == SqlTokenKind.Variable
            && (string.Equals(clause, "DECLARE", StringComparison.OrdinalIgnoreCase) || IsRoutineHeader(text, tokens, index)))
            return (SqlContextKind.DataType, []);

        if (clause is null)
            return (SqlContextKind.General, []);

        if (ExpressionClauses.Contains(clause))
            return (SqlContextKind.Column, []);

        if (TableSourceKeywords.Contains(clause))
            return (AfterTableSource(text, tokens, index, wordStart, clause, isInsideParen), []);

        return (SqlContextKind.General, []);
    }

    /// <summary>Remonte "a.b." depuis le point : parties nommees, la plus a gauche en premier.</summary>
    private static List<string> ReadQualifier(string text, SqlToken[] tokens, int dotIndex)
    {
        List<string> parts = [];
        var i = dotIndex;

        while (i >= 1 && tokens[i].Kind == SqlTokenKind.Dot && IsName(tokens[i - 1]) && tokens[i - 1].End == tokens[i].Start)
        {
            parts.Insert(0, Unquote(tokens[i - 1].GetText(text)));
            i -= 2;
        }

        return parts;
    }

    /// <summary>
    /// "AS" introduit un type dans CAST(x AS |), un alias dans SELECT / FROM, et le corps
    /// d'une vue ou d'une procedure (CREATE VIEW v AS |).
    /// </summary>
    private static SqlContextKind AfterAs(string text, SqlToken[] tokens, int asIndex)
    {
        var (clause, isInsideParen) = FindClause(text, tokens, asIndex);
        if (isInsideParen && FindOpenParenFunction(text, tokens, asIndex) is { } function && CastFunctions.Contains(function))
            return SqlContextKind.DataType;

        return clause is not null && (ExpressionClauses.Contains(clause) || TableSourceKeywords.Contains(clause))
            ? SqlContextKind.None
            : SqlContextKind.General;
    }

    /// <summary>
    /// Dans FROM / JOIN : apres une virgule on attend une table ; juste apres le nom d'une table,
    /// sur la meme ligne, un alias ou la suite de la requete (WHERE, JOIN...) ; sinon tout.
    /// </summary>
    private static SqlContextKind AfterTableSource(string text, SqlToken[] tokens, int index, int wordStart, string clause, bool isInsideParen)
    {
        var previous = tokens[index - 1];

        // INSERT INTO t (|  : liste de colonnes.
        if (isInsideParen && clause.Equals("INTO", StringComparison.OrdinalIgnoreCase))
            return SqlContextKind.Column;

        if (previous.Kind == SqlTokenKind.Comma)
            return isInsideParen ? SqlContextKind.Column : SqlContextKind.TableSource;

        // UPDATE t | et INTO t | n'acceptent pas d'alias : on y attend SET, VALUES...
        var isNameEnd = IsName(previous) || previous.Kind == SqlTokenKind.CloseParen;
        if (isNameEnd && AliasedSources.Contains(clause)
            && IsTableNamePosition(text, tokens, index - 1) && !ContainsNewLine(text, previous.End, wordStart))
            return SqlContextKind.AfterTableName;

        return SqlContextKind.General;
    }

    /// <summary>Vrai si le jeton termine le nom (eventuellement qualifie) qui suit FROM, JOIN, une virgule...</summary>
    private static bool IsTableNamePosition(string text, SqlToken[] tokens, int nameIndex)
    {
        var i = nameIndex;
        while (i >= 2 && tokens[i - 1].Kind == SqlTokenKind.Dot && IsName(tokens[i - 2]))
            i -= 2;

        if (i == 0)
            return false;

        var before = tokens[i - 1];
        return before.Kind == SqlTokenKind.Comma || IsWordIn(text, before, TableSourceKeywords);
    }

    /// <summary>CREATE PROCEDURE p @a |, (@a |, @b ...) : parametres types.</summary>
    private static bool IsRoutineHeader(string text, SqlToken[] tokens, int index)
    {
        for (var i = index - 1; i >= 0; i--)
        {
            if (tokens[i].IsWord(text, "AS") || tokens[i].IsWord(text, "BEGIN") || IsWordIn(text, tokens[i], ExpressionClauses))
                return false;

            if (tokens[i].IsWord(text, "PROCEDURE") || tokens[i].IsWord(text, "PROC") || tokens[i].IsWord(text, "FUNCTION"))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Mot-cle de clause le plus proche en remontant au meme niveau de parentheses ;
    /// une parenthese ouvrante non fermee fait remonter d'un niveau (COUNT(| -> SELECT).
    /// </summary>
    private static (string? Clause, bool IsInsideParen) FindClause(string text, SqlToken[] tokens, int index)
    {
        var depth = 0;
        var isInsideParen = false;

        for (var i = index - 1; i >= 0; i--)
        {
            var token = tokens[i];
            switch (token.Kind)
            {
                case SqlTokenKind.CloseParen:
                    depth++;
                    break;
                case SqlTokenKind.OpenParen when depth > 0:
                    depth--;
                    break;
                case SqlTokenKind.OpenParen:
                    isInsideParen = true;
                    break;
                case SqlTokenKind.Semicolon when depth == 0:
                    return (null, isInsideParen);
                case SqlTokenKind.Word when depth == 0:
                    var word = token.GetText(text);
                    if (ExpressionClauses.Contains(word) || TableSourceKeywords.Contains(word) || StatementClauses.Contains(word))
                        return (word, isInsideParen);
                    break;
            }
        }

        return (null, isInsideParen);
    }

    /// <summary>Nom de la fonction dont la parenthese ouvrante englobe le jeton.</summary>
    private static string? FindOpenParenFunction(string text, SqlToken[] tokens, int index)
    {
        var depth = 0;
        for (var i = index - 1; i >= 0; i--)
        {
            if (tokens[i].Kind == SqlTokenKind.CloseParen)
                depth++;
            else if (tokens[i].Kind == SqlTokenKind.OpenParen && depth-- == 0)
                return i > 0 && tokens[i - 1].Kind == SqlTokenKind.Word ? tokens[i - 1].GetText(text) : null;
        }

        return null;
    }

    // ---------- Tables et base ----------

    /// <summary>Tables apres FROM, JOIN, UPDATE, INTO, MERGE, USING, APPLY, et apres les virgules d'un FROM.</summary>
    private static List<SqlTableReference> ParseTables(string text, SqlToken[] tokens)
    {
        List<SqlTableReference> tables = [];

        for (var i = 0; i < tokens.Length; i++)
        {
            if (!IsWordIn(text, tokens[i], TableSourceKeywords) || tokens[i].IsWord(text, "TABLE"))
                continue;

            var isFrom = tokens[i].IsWord(text, "FROM");
            var next = i + 1;

            while (TryParseTable(text, tokens, next, out var table, out next))
            {
                if (table is not null)
                    tables.Add(table);

                // FROM a, b, c : chaque virgule de premier niveau ajoute une table.
                if (!isFrom || next >= tokens.Length || tokens[next].Kind != SqlTokenKind.Comma)
                    break;

                next++;
            }
        }

        return tables;
    }

    /// <summary>
    /// Lit "[db.][schema.]nom [(args)] [AS] [alias] [WITH (hints)]" ; une sous-requete
    /// (SELECT ...) alias est sautee sans produire de table.
    /// </summary>
    private static bool TryParseTable(string text, SqlToken[] tokens, int start, out SqlTableReference? table, out int next)
    {
        table = null;
        next = start;

        if (next >= tokens.Length)
            return false;

        List<string> parts = [];
        var offset = tokens[next].Start;

        if (tokens[next].Kind == SqlTokenKind.OpenParen)
        {
            next = SkipParens(tokens, next);
        }
        else
        {
            while (next < tokens.Length && IsName(tokens[next]) && !IsWordIn(text, tokens[next], ReservedWords))
            {
                parts.Add(Unquote(tokens[next].GetText(text)));
                next++;

                if (next < tokens.Length && tokens[next].Kind == SqlTokenKind.Dot)
                    next++;
                else
                    break;
            }

            if (parts.Count == 0)
                return false;

            // Fonction table : dbo.fn(@x) alias.
            if (next < tokens.Length && tokens[next].Kind == SqlTokenKind.OpenParen)
                next = SkipParens(tokens, next);
        }

        if (next < tokens.Length && tokens[next].IsWord(text, "AS"))
            next++;

        string? alias = null;
        if (next < tokens.Length && IsName(tokens[next]) && !IsWordIn(text, tokens[next], ReservedWords))
        {
            alias = Unquote(tokens[next].GetText(text));
            next++;
        }

        if (next + 1 < tokens.Length && tokens[next].IsWord(text, "WITH") && tokens[next + 1].Kind == SqlTokenKind.OpenParen)
            next = SkipParens(tokens, next + 1);

        if (parts.Count > 0)
        {
            var name = parts[^1];
            var schema = parts.Count >= 2 ? NullIfEmpty(parts[^2]) : null;
            var database = parts.Count >= 3 ? NullIfEmpty(parts[^3]) : null;
            table = new SqlTableReference(database, schema, name, alias, offset);
        }

        return true;
    }

    /// <summary>Dernier USE avant le curseur, tous lots confondus.</summary>
    private static string? FindDatabase(string text, IReadOnlyList<SqlToken> tokens, int caret)
    {
        string? database = null;
        SqlToken? previous = null;

        foreach (var token in tokens.Where(t => !t.IsComment && t.End <= caret))
        {
            if (previous is { } use && use.IsWord(text, "USE") && IsName(token))
                database = Unquote(token.GetText(text));

            previous = token;
        }

        return database;
    }

    // ---------- Utilitaires ----------

    private static int SkipParens(SqlToken[] tokens, int openIndex)
    {
        var depth = 0;
        for (var i = openIndex; i < tokens.Length; i++)
        {
            if (tokens[i].Kind == SqlTokenKind.OpenParen)
                depth++;
            else if (tokens[i].Kind == SqlTokenKind.CloseParen && --depth == 0)
                return i + 1;
        }

        return tokens.Length;
    }

    private static bool IsName(SqlToken token) => token.Kind is SqlTokenKind.Word or SqlTokenKind.QuotedIdentifier;

    private static bool IsWordIn(string text, SqlToken token, HashSet<string> words)
        => token.Kind == SqlTokenKind.Word && words.Contains(token.GetText(text));

    private static bool ContainsNewLine(string text, int start, int end)
        => end > start && text.AsSpan(start, end - start).Contains('\n');

    private static string? NullIfEmpty(string value) => value.Length == 0 ? null : value;

    /// <summary>[Ma Table] -> Ma Table, "x""y" -> x"y.</summary>
    public static string Unquote(string name)
    {
        if (name.Length >= 2 && name[0] == '[')
            return name[1..(name[^1] == ']' ? ^1 : ^0)].Replace("]]", "]", StringComparison.Ordinal);

        if (name.Length >= 2 && name[0] == '"')
            return name[1..(name[^1] == '"' ? ^1 : ^0)].Replace("\"\"", "\"", StringComparison.Ordinal);

        return name;
    }
}
