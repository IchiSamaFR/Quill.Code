namespace Quill.Code.Sql.SqlServer.Completion;

/// <summary>T-SQL keywords, types and functions of <see cref="SqlServerDialect"/>.</summary>
internal static class SqlKeywords
{
    public static IReadOnlyList<string> Keywords { get; } =
    [
        "SELECT", "FROM", "WHERE", "GROUP", "BY", "HAVING", "ORDER", "ASC", "DESC", "TOP", "DISTINCT", "INSERT", "INTO", "VALUES",
        "UPDATE", "SET", "DELETE", "MERGE", "OUTPUT", "TRUNCATE", "CREATE", "ALTER", "DROP", "TABLE", "VIEW", "INDEX", "PROCEDURE",
        "PROC", "FUNCTION", "TRIGGER", "SCHEMA", "DATABASE", "CONSTRAINT", "PRIMARY", "FOREIGN", "KEY", "REFERENCES", "UNIQUE",
        "CHECK", "DEFAULT", "IDENTITY", "CLUSTERED", "NONCLUSTERED", "JOIN", "INNER", "LEFT", "RIGHT", "FULL", "OUTER", "CROSS",
        "APPLY", "ON", "AS", "UNION", "ALL", "EXCEPT", "INTERSECT", "AND", "OR", "NOT", "IN", "EXISTS", "BETWEEN", "LIKE", "IS",
        "NULL", "CASE", "WHEN", "THEN", "ELSE", "END", "IIF", "WITH", "OVER", "PARTITION", "ROWS", "RANGE", "DECLARE", "EXEC",
        "EXECUTE", "RETURN", "BEGIN", "IF", "WHILE", "BREAK", "CONTINUE", "GOTO", "TRY", "CATCH", "THROW", "RAISERROR",
        "TRANSACTION", "TRAN", "COMMIT", "ROLLBACK", "SAVE", "GRANT", "REVOKE", "DENY", "USE", "GO", "OFFSET", "FETCH", "NEXT",
        "ONLY", "FIRST", "NOLOCK", "READUNCOMMITTED",
    ];

    public static IReadOnlyList<string> DataTypes { get; } =
    [
        "INT", "BIGINT", "SMALLINT", "TINYINT", "BIT", "DECIMAL", "NUMERIC", "MONEY", "FLOAT", "REAL", "DATE", "DATETIME",
        "DATETIME2", "DATETIMEOFFSET", "TIME", "CHAR", "VARCHAR", "NCHAR", "NVARCHAR", "TEXT", "NTEXT", "BINARY", "VARBINARY",
        "UNIQUEIDENTIFIER", "XML", "SQL_VARIANT", "MAX",
    ];

    public static IReadOnlyList<string> Functions { get; } =
    [
        "COUNT", "SUM", "AVG", "MIN", "MAX", "CAST", "CONVERT", "TRY_CAST", "TRY_CONVERT", "ISNULL", "COALESCE", "NULLIF", "LEN",
        "SUBSTRING", "REPLACE", "LTRIM", "RTRIM", "TRIM", "UPPER", "LOWER", "CONCAT", "FORMAT", "GETDATE", "SYSDATETIME",
        "DATEADD", "DATEDIFF", "DATEPART", "YEAR", "MONTH", "DAY", "ROW_NUMBER", "RANK", "DENSE_RANK", "NTILE", "LAG", "LEAD",
        "STRING_AGG", "NEWID", "SCOPE_IDENTITY",
    ];
}
