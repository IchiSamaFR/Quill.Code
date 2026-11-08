using Quill.Code.Sql.SqlServer.Lexing;
using System.Text.RegularExpressions;

namespace Quill.Code.Sql.SqlServer.Completion;

/// <summary>
/// Guesses what T-SQL expects at the caret, without a real parser: previous token first,
/// then nearest clause keyword at the same parenthesis level. The analysis is limited
/// to the current batch (delimited by GO), like the execution.
/// </summary>
public static partial class SqlContextAnalyzer
{
    /// <summary>Words after which a table name is expected.</summary>
    private static readonly HashSet<string> TableSourceKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "FROM", "JOIN", "UPDATE", "INTO", "TABLE", "MERGE", "USING", "APPLY"
    };

    /// <summary>Sources where an alias can follow the table name.</summary>
    private static readonly HashSet<string> AliasedSources = new(StringComparer.OrdinalIgnoreCase)
    {
        "FROM", "JOIN", "MERGE", "USING", "APPLY"
    };

    /// <summary>Clauses whose content is an expression (columns, functions).</summary>
    private static readonly HashSet<string> ExpressionClauses = new(StringComparer.OrdinalIgnoreCase)
    {
        "SELECT", "WHERE", "ON", "HAVING", "BY", "SET", "VALUES", "OUTPUT", "IF", "WHILE", "RETURN", "PRINT"
    };

    /// <summary>Clauses opening a new statement.</summary>
    private static readonly HashSet<string> StatementClauses = new(StringComparer.OrdinalIgnoreCase)
    {
        "UNION", "EXCEPT", "INTERSECT", "BEGIN", "END", "DECLARE", "EXEC", "EXECUTE"
    };

    /// <summary>Words that cannot be a table name or alias.</summary>
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

    /// <summary>Rest of the line after GO: a repetition count and a comment, nothing else.</summary>
    [GeneratedRegex(@"^[ \t]*\d*[ \t]*(?:--.*)?\r?$", RegexOptions.CultureInvariant)]
    private static partial Regex GoLineTailRegex();

    /// <summary>Analyzes the text around <paramref name="caret"/>.</summary>
    /// <param name="text">Full editor text.</param>
    /// <param name="caret">Caret position.</param>
    /// <param name="database">Database imposed by the caller (dashboard…), if the text does not choose one with USE.</param>
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

        // Tokens strictly before the word being typed.
        var index = tokens.Count(t => t.End <= wordStart);

        var (kind, qualifier) = DetermineKind(text, tokens, index, wordStart);
        if (kind == SqlContextKind.None)
            return SqlContext.None;

        // The qualifier's first word is analyzed as if it were being typed.
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
            // A USE in the text wins over the database imposed by the caller (dashboard).
            Database = FindDatabase(text, all, caret) ?? (string.IsNullOrWhiteSpace(database) ? null : database)
        };
    }

    /// <summary>
    /// Tables and database of the batch containing <paramref name="offset"/>, without guessing what is expected:
    /// the tooltip needs them on an already written name, where <see cref="Analyze"/> suggests nothing.
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

    /// <summary>Start of the word being typed (@ and # for variables and temporary tables).</summary>
    public static int FindWordStart(string text, int caret)
    {
        var start = Math.Clamp(caret, 0, text.Length);
        while (start > 0 && IsIdentifierChar(text[start - 1]))
            start--;

        return start;
    }

    private static bool IsIdentifierChar(char c) => char.IsLetterOrDigit(c) || c is '_' or '@' or '#' or '$';

    // ---------- Caret position ----------

    private static bool IsCaretInside(SqlToken token, int caret)
    {
        if (token.Kind is not (SqlTokenKind.String or SqlTokenKind.QuotedIdentifier or SqlTokenKind.LineComment or SqlTokenKind.BlockComment))
            return false;

        return token.Start < caret && (caret < token.End || (caret == token.End && !token.IsTerminated));
    }

    /// <summary>Bounds of the batch containing the caret: between the GO lines around it.</summary>
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

    // ---------- Expected kind ----------

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

        // CONVERT(type, value): the type comes first.
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

    /// <summary>Walks "a.b." back from the dot: named parts, leftmost first.</summary>
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
    /// "AS" introduces a type in CAST(x AS |), an alias in SELECT / FROM, and the body
    /// of a view or procedure (CREATE VIEW v AS |).
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
    /// In FROM / JOIN: after a comma a table is expected; right after a table name,
    /// on the same line, an alias or the rest of the query (WHERE, JOIN...); otherwise anything.
    /// </summary>
    private static SqlContextKind AfterTableSource(string text, SqlToken[] tokens, int index, int wordStart, string clause, bool isInsideParen)
    {
        var previous = tokens[index - 1];

        // INSERT INTO t (|  : column list.
        if (isInsideParen && clause.Equals("INTO", StringComparison.OrdinalIgnoreCase))
            return SqlContextKind.Column;

        if (previous.Kind == SqlTokenKind.Comma)
            return isInsideParen ? SqlContextKind.Column : SqlContextKind.TableSource;

        // UPDATE t | and INTO t | do not accept an alias: SET, VALUES... are expected there.
        var isNameEnd = IsName(previous) || previous.Kind == SqlTokenKind.CloseParen;
        if (isNameEnd && AliasedSources.Contains(clause)
            && IsTableNamePosition(text, tokens, index - 1) && !ContainsNewLine(text, previous.End, wordStart))
            return SqlContextKind.AfterTableName;

        return SqlContextKind.General;
    }

    /// <summary>True if the token ends the (possibly qualified) name following FROM, JOIN, a comma...</summary>
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

    /// <summary>CREATE PROCEDURE p @a |, (@a |, @b ...): typed parameters.</summary>
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
    /// Nearest clause keyword walking back at the same parenthesis level;
    /// an unclosed opening parenthesis moves up one level (COUNT(| -> SELECT).
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

    /// <summary>Name of the function whose opening parenthesis encloses the token.</summary>
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

    // ---------- Tables and database ----------

    /// <summary>Tables after FROM, JOIN, UPDATE, INTO, MERGE, USING, APPLY, and after the commas of a FROM.</summary>
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

                // FROM a, b, c: each top-level comma adds a table.
                if (!isFrom || next >= tokens.Length || tokens[next].Kind != SqlTokenKind.Comma)
                    break;

                next++;
            }
        }

        return tables;
    }

    /// <summary>
    /// Reads "[db.][schema.]name [(args)] [AS] [alias] [WITH (hints)]"; a subquery
    /// (SELECT ...) alias is skipped without producing a table.
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

            // Table function: dbo.fn(@x) alias.
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

    /// <summary>Last USE before the caret, across all batches.</summary>
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

    // ---------- Utilities ----------

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

    /// <summary>[My Table] -> My Table, "x""y" -> x"y.</summary>
    public static string Unquote(string name)
    {
        if (name.Length >= 2 && name[0] == '[')
            return name[1..(name[^1] == ']' ? ^1 : ^0)].Replace("]]", "]", StringComparison.Ordinal);

        if (name.Length >= 2 && name[0] == '"')
            return name[1..(name[^1] == '"' ? ^1 : ^0)].Replace("\"\"", "\"", StringComparison.Ordinal);

        return name;
    }
}
