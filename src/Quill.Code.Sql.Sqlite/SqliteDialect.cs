using Quill.Code.Sql.Constants;
using Quill.Code.Sql.Dialects;
using Quill.Code.Sql.Sqlite.Constants;

namespace Quill.Code.Sql.Sqlite;

/// <summary>SQLite dialect: <c>"name"</c>, <c>@param</c>, <c>LIMIT n</c>, database.object (main, attached databases).</summary>
public static class SqliteDialect
{
    /// <summary>SQLite dialect.</summary>
    public static SqlDialect Instance { get; } = new()
    {
        Name = "SQLite",
        IdentifierQuotes = [new SqlIdentifierQuote('"', '"'), new SqlIdentifierQuote('`', '`'), new SqlIdentifierQuote('[', ']')],
        ParameterPrefix = '@',
        RowLimit = SqlRowLimitSyntax.Limit,
        SchemaIsDatabase = true,
        QueriesOtherDatabases = true,
        DefaultSchema = "main",
        ProcedureKeywords = [],
        Keywords = [.. SqlStandardKeywords.Keywords, .. SqliteKeywords.Keywords],
        DataTypes = [.. SqlStandardKeywords.DataTypes, .. SqliteKeywords.DataTypes],
        Functions = [.. SqlStandardKeywords.Functions, .. SqliteKeywords.Functions],
        ClauseKeywords = SqliteKeywords.ClauseKeywords,
    };
}
