using Quill.Code.Sql.Constants;
using Quill.Code.Sql.Dialects;
using Quill.Code.Sql.MySql.Constants;

namespace Quill.Code.Sql.MySql;

/// <summary>MySQL / MariaDB dialect: <c>`name`</c>, <c>@param</c>, <c>LIMIT n</c>, database.object, <c>#</c> comments.</summary>
public static class MySqlDialect
{
    /// <summary>MySQL / MariaDB dialect.</summary>
    public static SqlDialect Instance { get; } = new()
    {
        Name = "MySQL",
        IdentifierQuotes = [new SqlIdentifierQuote('`', '`')],
        DoubleQuotedStrings = true,
        ParameterPrefix = '@',
        RowLimit = SqlRowLimitSyntax.Limit,
        SchemaIsDatabase = true,
        QueriesOtherDatabases = true,
        HasUseStatement = true,
        ProcedureKeywords = ["CALL"],
        HashLineComments = true,
        Keywords = [.. SqlStandardKeywords.Keywords, .. MySqlKeywords.Keywords],
        DataTypes = [.. SqlStandardKeywords.DataTypes, .. MySqlKeywords.DataTypes],
        Functions = [.. SqlStandardKeywords.Functions, .. MySqlKeywords.Functions],
        ClauseKeywords = MySqlKeywords.ClauseKeywords,
    };
}
