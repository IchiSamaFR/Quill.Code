using Quill.Code.Analysis;
using Quill.Code.Sql.SqlServer.Completion;
using Quill.Code.Sql.SqlServer.Lexing;
using Quill.Code.Sql.SqlServer.Schema;
using System.Text;
using Quill.Data.Sql;
using Quill.Code.Sql.SqlServer.Constants;

namespace Quill.Code.Sql.SqlServer.Analysis;

/// <summary>
/// T-SQL hover: type of a column, columns of a table or alias, object, schema or database (host catalog),
/// type of a variable declared in the text or provided by the host.
/// </summary>
internal sealed class SqlHoverService : ICodeHover
{
    /// <summary>Words ending a DECLARE statement.</summary>
    private static readonly HashSet<string> StatementWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "SELECT", "SET", "DECLARE", "INSERT", "UPDATE", "DELETE", "MERGE", "IF", "WHILE", "BEGIN", "END",
        "EXEC", "EXECUTE", "RETURN", "PRINT", "WITH", "GO", "USE",
    };

    private readonly SqlLanguageOptions _options;
    private readonly SqlCatalogReader _catalog;

    public SqlHoverService(SqlLanguageOptions options)
    {
        _options = options;
        _catalog = new SqlCatalogReader(options.Catalog);
    }

    /// <inheritdoc />
    public async Task<HoverInfo?> GetHoverAsync(string code, int position, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(code);

        // Comments skipped: nothing to explain inside, and they would break the qualifier (a./*x*/b).
        var tokens = SqlLexer.Tokenize(code).Where(t => !t.IsComment).ToArray();

        // The hovered character, or the one before it (mouse placed right after the end of the name).
        var index = Array.FindIndex(tokens, t => t.Start <= position && position < t.End);
        if (index < 0)
            index = Array.FindIndex(tokens, t => t.Start < position && position == t.End);
        if (index < 0)
            return null;

        var token = tokens[index];
        if (token.Kind == SqlTokenKind.Variable)
            return DescribeVariable(code, tokens, token);

        if (token.Kind is not (SqlTokenKind.Word or SqlTokenKind.QuotedIdentifier) || !_catalog.IsAvailable)
            return null;

        var scope = SqlContextAnalyzer.AnalyzeScope(code, token.Start, _options.Database);
        var name = SqlContextAnalyzer.Unquote(token.GetText(code));
        var described = await DescribeNameAsync(scope, ReadQualifier(code, tokens, index), name, cancellationToken).ConfigureAwait(false);

        return described is { } hover ? new HoverInfo(token.Start, token.Length, hover.Signature, hover.Description) : null;
    }

    /// <summary>Parts before the name: ["u"] for u.Name, ["dbo", "Users"] for dbo.Users.Name.</summary>
    private static List<string> ReadQualifier(string text, SqlToken[] tokens, int index)
    {
        List<string> parts = [];
        for (var i = index - 1; i >= 1 && tokens[i].Kind == SqlTokenKind.Dot && tokens[i - 1].Kind is SqlTokenKind.Word or SqlTokenKind.QuotedIdentifier; i -= 2)
            parts.Insert(0, SqlContextAnalyzer.Unquote(tokens[i - 1].GetText(text)));

        return parts;
    }

    // ---------- Catalog names ----------

    private async Task<(string Signature, string? Description)?> DescribeNameAsync(SqlContext scope, List<string> qualifier, string name, CancellationToken cancellationToken)
    {
        switch (qualifier.Count)
        {
            case 0:
                {
                    // Alias or table of the FROM, then column of a table of the batch, then object, schema or database.
                    if (scope.FindTable(name) is { } reference && await _catalog.FindAsync(scope, reference, cancellationToken).ConfigureAwait(false) is { } referenced)
                        return DescribeObject(referenced, reference.Alias);

                    List<(SqlSchemaObject Table, SqlColumn Column)> matches = [];
                    foreach (var table in scope.Tables)
                    {
                        if (await _catalog.FindAsync(scope, table, cancellationToken).ConfigureAwait(false) is { } source
                            && FindColumn(source, name) is { } column
                            && !matches.Any(m => ReferenceEquals(m.Table, source)))
                            matches.Add((source, column));
                    }

                    if (matches.Count > 0)
                        return DescribeColumn(matches[0].Table, matches[0].Column, matches.Skip(1).Select(m => m.Table.QualifiedName).ToArray());

                    var current = await _catalog.GetDatabaseAsync(scope.Database, cancellationToken).ConfigureAwait(false);
                    if (current?.Find(null, name) is { } direct)
                        return DescribeObject(direct, null);

                    if (current is not null && current.HasSchema(name))
                        return ($"(schema) {name}", $"Database {current.Name}");

                    return await _catalog.IsDatabaseAsync(name, cancellationToken).ConfigureAwait(false) ? ($"(database) {name}", null) : null;
                }

            case 1:
                {
                    var owner = qualifier[0];
                    if (scope.FindTable(owner) is { } reference && await _catalog.FindAsync(scope, reference, cancellationToken).ConfigureAwait(false) is { } aliased)
                        return ColumnOf(aliased, name);

                    var current = await _catalog.GetDatabaseAsync(scope.Database, cancellationToken).ConfigureAwait(false);
                    if (current is not null && current.HasSchema(owner) && current.Find(owner, name) is { } inSchema)
                        return DescribeObject(inSchema, null);

                    if (current?.Find(null, owner) is { } table)
                        return ColumnOf(table, name);

                    if (await _catalog.IsDatabaseAsync(owner, cancellationToken).ConfigureAwait(false)
                        && (await _catalog.GetDatabaseAsync(owner, cancellationToken).ConfigureAwait(false))?.HasSchema(name) == true)
                        return ($"(schema) {owner}.{name}", $"Database {owner}");

                    return null;
                }

            case 2:
                {
                    var current = await _catalog.GetDatabaseAsync(scope.Database, cancellationToken).ConfigureAwait(false);
                    if (current?.Find(qualifier[0], qualifier[1]) is { } table)
                        return ColumnOf(table, name);

                    if (await _catalog.IsDatabaseAsync(qualifier[0], cancellationToken).ConfigureAwait(false)
                        && (await _catalog.GetDatabaseAsync(qualifier[0], cancellationToken).ConfigureAwait(false))?.Find(qualifier[1], name) is { } remote)
                        return DescribeObject(remote, null);

                    return null;
                }

            case 3:
                {
                    var other = await _catalog.GetDatabaseAsync(qualifier[0], cancellationToken).ConfigureAwait(false);
                    return other?.Find(qualifier[1], qualifier[2]) is { } table ? ColumnOf(table, name) : null;
                }

            default:
                return null;
        }
    }

    private static SqlColumn? FindColumn(SqlSchemaObject table, string name)
        => table.Columns.FirstOrDefault(c => string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase));

    private static (string, string?)? ColumnOf(SqlSchemaObject table, string name)
        => FindColumn(table, name) is { } column ? DescribeColumn(table, column, []) : null;

    // ---------- Texts ----------

    /// <summary>E.g. "(column) Name nvarchar(50) NOT NULL", then the table (and the other tables of the batch that also have it).</summary>
    private static (string, string?) DescribeColumn(SqlSchemaObject table, SqlColumn column, IReadOnlyList<string> alsoIn)
    {
        var description = new StringBuilder(table.QualifiedName);
        if (column.IsPrimaryKey)
            description.Append(" · primary key");
        if (alsoIn.Count > 0)
            description.Append("\nAlso in: ").AppendJoin(", ", alsoIn);

        return ($"(column) {column.Name} {column.DataType} {(column.IsNullable ? "NULL" : "NOT NULL")}", description.ToString());
    }

    /// <summary>E.g. "(table) dbo.Users", then its first columns with their type.</summary>
    private static (string, string?) DescribeObject(SqlSchemaObject source, string? alias)
    {
        var kind = source.Kind switch
        {
            SqlSchemaObjectKind.View => "view",
            SqlSchemaObjectKind.Procedure => "procedure",
            _ => "table",
        };

        var signature = alias is null || string.Equals(alias, source.Name, StringComparison.OrdinalIgnoreCase)
            ? $"({kind}) {source.QualifiedName}"
            : $"({kind}) {source.QualifiedName} AS {alias}";

        if (source.Columns.Count == 0)
            return (signature, null);

        var description = string.Join('\n', source.Columns
            .Take(SqlAnalysisConstants.HoverMaxListedColumns)
            .Select(c => $"{c.Name} {c.DataType}{(c.IsPrimaryKey ? " · key" : string.Empty)}"));

        if (source.Columns.Count > SqlAnalysisConstants.HoverMaxListedColumns)
            description += $"\n… and {source.Columns.Count - SqlAnalysisConstants.HoverMaxListedColumns} more";

        return (signature, description);
    }

    // ---------- Variables ----------

    private HoverInfo? DescribeVariable(string text, SqlToken[] tokens, SqlToken token)
    {
        var name = token.GetText(text);

        var variable = _options.Variables.FirstOrDefault(v => string.Equals(v.Reference, "@" + name.TrimStart('@'), StringComparison.OrdinalIgnoreCase));
        if (variable is not null)
            return new HoverInfo(token.Start, token.Length, $"(parameter) {name} {variable.Type}", variable.Description);

        return FindDeclaredType(text, tokens, name) is { } type
            ? new HoverInfo(token.Start, token.Length, $"(variable) {name} {type}", null)
            : null;
    }

    /// <summary>Type given by a DECLARE @a int, @b nvarchar(50) = 'x' in the text; null if the variable is not declared there.</summary>
    private static string? FindDeclaredType(string text, SqlToken[] tokens, string name)
    {
        for (var i = 0; i < tokens.Length; i++)
        {
            if (!tokens[i].IsWord(text, "DECLARE"))
                continue;

            var j = i + 1;
            while (j < tokens.Length && tokens[j].Kind == SqlTokenKind.Variable)
            {
                var variable = tokens[j].GetText(text);
                j++;

                if (j < tokens.Length && tokens[j].IsWord(text, "AS"))
                    j++;

                if (j >= tokens.Length)
                    break;

                var typeStart = tokens[j].Start;
                var isTable = tokens[j].IsWord(text, "TABLE");
                var typeEnd = tokens[j].End;
                j++;

                if (j < tokens.Length && tokens[j].Kind == SqlTokenKind.OpenParen)
                {
                    var close = SkipParens(tokens, j);
                    typeEnd = tokens[close - 1].End;
                    j = close;
                }

                if (string.Equals(variable, name, StringComparison.OrdinalIgnoreCase))
                    return isTable ? "TABLE" : string.Join(' ', text[typeStart..typeEnd].Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

                // Optional initial value, up to the comma announcing the next variable.
                j = SkipToNextVariable(text, tokens, j);
            }
        }

        return null;
    }

    /// <summary>Index of the next variable of the same DECLARE, or past the end if the statement stops.</summary>
    private static int SkipToNextVariable(string text, SqlToken[] tokens, int index)
    {
        var depth = 0;
        for (var i = index; i < tokens.Length; i++)
        {
            var token = tokens[i];
            if (token.Kind == SqlTokenKind.OpenParen)
                depth++;
            else if (token.Kind == SqlTokenKind.CloseParen)
                depth--;
            else if (depth == 0 && token.Kind == SqlTokenKind.Comma && i + 1 < tokens.Length && tokens[i + 1].Kind == SqlTokenKind.Variable)
                return i + 1;
            else if (depth == 0 && (token.Kind == SqlTokenKind.Semicolon || (token.Kind == SqlTokenKind.Word && StatementWords.Contains(token.GetText(text)))))
                return tokens.Length;
        }

        return tokens.Length;
    }

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
}
