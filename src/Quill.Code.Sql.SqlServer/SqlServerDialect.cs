using Quill.Code.Sql.Dialects;
using Quill.Code.Sql.SqlServer.Completion;

namespace Quill.Code.Sql.SqlServer;

/// <summary>T-SQL dialect: <c>[name]</c>, <c>@param</c>, <c>SELECT TOP (n)</c>, database.schema.object, batches separated by GO.</summary>
public static class SqlServerDialect
{
    /// <summary>SQL Server dialect.</summary>
    public static SqlDialect Instance { get; } = new()
    {
        Name = "SQL Server",
        IdentifierQuotes = [new SqlIdentifierQuote('[', ']'), new SqlIdentifierQuote('"', '"')],
        ExtraIdentifierChars = "@#$",
        ParameterPrefix = '@',
        RowLimit = SqlRowLimitSyntax.Top,
        QualifiesWithDatabase = true,
        QueriesOtherDatabases = true,
        DefaultSchema = "dbo",
        BatchSeparator = "GO",
        HasUseStatement = true,
        ProcedureKeywords = ["EXEC", "EXECUTE"],
        NestedBlockComments = true,
        Keywords = SqlKeywords.Keywords,
        DataTypes = SqlKeywords.DataTypes,
        Functions = SqlKeywords.Functions,
    };
}
