namespace Quill.Code.Sql.Sqlite.Constants;

/// <summary>SQLite words added to the shared SQL.</summary>
internal static class SqliteKeywords
{
    internal static IReadOnlyList<string> Keywords { get; } =
    [
        "LIMIT", "OFFSET", "PRAGMA", "VACUUM", "ATTACH", "DETACH", "IGNORE", "ABORT", "FAIL", "CONFLICT", "NOTHING", "DO",
        "AUTOINCREMENT", "GLOB", "REGEXP", "MATCH", "RETURNING", "EXPLAIN", "QUERY", "PLAN", "TEMP", "TEMPORARY", "VIRTUAL",
        "WITHOUT", "ROWID", "STRICT", "INDEXED", "REINDEX", "ANALYZE", "RENAME", "COLUMN", "ADD", "IF", "NULLS", "FIRST", "LAST",
        "WINDOW", "FILTER", "COLLATE", "NOCASE", "DEFERRED", "IMMEDIATE", "EXCLUSIVE", "RELEASE",
    ];

    internal static IReadOnlyList<string> DataTypes { get; } = ["TEXT", "BLOB", "BOOLEAN", "DATETIME"];

    internal static IReadOnlyList<string> Functions { get; } =
    [
        "IFNULL", "IIF", "LENGTH", "SUBSTR", "INSTR", "CONCAT", "PRINTF", "FORMAT", "DATE", "DATETIME", "STRFTIME", "JULIANDAY",
        "UNIXEPOCH", "RANDOM", "RANDOMBLOB", "HEX", "TYPEOF", "TOTAL", "GROUP_CONCAT", "LAST_INSERT_ROWID", "CHANGES",
        "JSON_EXTRACT", "JSON_OBJECT", "JSON_ARRAY",
    ];

    internal static IReadOnlyList<string> ClauseKeywords { get; } = ["LIMIT", "NATURAL", "USING", "RETURNING", "INDEXED", "WINDOW"];
}
