using Quill.Code.Sql.Constants;
using Quill.Code.Sql.Dialects;
using Quill.Code.Sql.PostgreSql.Constants;

namespace Quill.Code.Sql.PostgreSql;

/// <summary>PostgreSQL dialect: <c>"name"</c> (lower case when unquoted), <c>@param</c>, <c>LIMIT n</c>, schema.object, <c>$$</c> strings.</summary>
public static class PostgreSqlDialect
{
    /// <summary>PostgreSQL dialect.</summary>
    public static SqlDialect Instance { get; } = new()
    {
        Name = "PostgreSQL",
        IdentifierCasing = SqlIdentifierCasing.Lower,
        ParameterPrefix = '@',
        RowLimit = SqlRowLimitSyntax.Limit,
        DefaultSchema = "public",
        ProcedureKeywords = ["CALL"],
        NestedBlockComments = true,
        DollarQuotedStrings = true,
        Keywords = [.. SqlStandardKeywords.Keywords, .. PostgreSqlKeywords.Keywords],
        DataTypes = [.. SqlStandardKeywords.DataTypes, .. PostgreSqlKeywords.DataTypes],
        Functions = [.. SqlStandardKeywords.Functions, .. PostgreSqlKeywords.Functions],
        ClauseKeywords = PostgreSqlKeywords.ClauseKeywords,
        Phrases = PostgreSqlKeywords.Phrases,
    };
}
