namespace Quill.Code.Sql.Constants;

/// <summary>SQL words shared by the engines: each dialect adds its own.</summary>
public static class SqlStandardKeywords
{
    /// <summary>Keywords.</summary>
    public static IReadOnlyList<string> Keywords { get; } =
    [
        "SELECT", "FROM", "WHERE", "GROUP", "BY", "HAVING", "ORDER", "ASC", "DESC", "DISTINCT", "INSERT", "INTO", "VALUES",
        "UPDATE", "SET", "DELETE", "TRUNCATE", "CREATE", "ALTER", "DROP", "TABLE", "VIEW", "INDEX", "PROCEDURE", "FUNCTION",
        "TRIGGER", "SCHEMA", "DATABASE", "CONSTRAINT", "PRIMARY", "FOREIGN", "KEY", "REFERENCES", "UNIQUE", "CHECK", "DEFAULT",
        "JOIN", "INNER", "LEFT", "RIGHT", "FULL", "OUTER", "CROSS", "NATURAL", "USING", "ON", "AS", "UNION", "ALL", "EXCEPT",
        "INTERSECT", "AND", "OR", "NOT", "IN", "EXISTS", "BETWEEN", "LIKE", "IS", "NULL", "CASE", "WHEN", "THEN", "ELSE", "END",
        "WITH", "RECURSIVE", "OVER", "PARTITION", "ROWS", "RANGE", "BEGIN", "COMMIT", "ROLLBACK", "SAVEPOINT", "TRANSACTION",
        "GRANT", "REVOKE",
    ];

    /// <summary>Data types.</summary>
    public static IReadOnlyList<string> DataTypes { get; } =
    [
        "INT", "INTEGER", "BIGINT", "SMALLINT", "DECIMAL", "NUMERIC", "FLOAT", "REAL", "DATE", "TIME", "TIMESTAMP", "CHAR",
        "VARCHAR",
    ];

    /// <summary>Functions.</summary>
    public static IReadOnlyList<string> Functions { get; } =
    [
        "COUNT", "SUM", "AVG", "MIN", "MAX", "CAST", "COALESCE", "NULLIF", "SUBSTRING", "REPLACE", "TRIM", "LTRIM", "RTRIM",
        "UPPER", "LOWER", "ABS", "ROUND", "ROW_NUMBER", "RANK", "DENSE_RANK", "NTILE", "LAG", "LEAD",
    ];
}
