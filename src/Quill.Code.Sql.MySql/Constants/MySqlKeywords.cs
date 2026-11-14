namespace Quill.Code.Sql.MySql.Constants;

/// <summary>MySQL / MariaDB words added to the shared SQL.</summary>
internal static class MySqlKeywords
{
    internal static IReadOnlyList<string> Keywords { get; } =
    [
        "LIMIT", "OFFSET", "USE", "SHOW", "DESCRIBE", "EXPLAIN", "IGNORE", "DUPLICATE", "AUTO_INCREMENT", "ENGINE", "CHARSET",
        "COLLATE", "UNSIGNED", "ZEROFILL", "STRAIGHT_JOIN", "REGEXP", "RLIKE", "XOR", "DIV", "CALL", "DELIMITER", "IF", "ELSEIF",
        "WHILE", "DO", "LOOP", "REPEAT", "UNTIL", "LEAVE", "ITERATE", "DECLARE", "RETURN", "RETURNS", "LOCK", "UNLOCK", "TABLES",
        "START", "TEMPORARY", "INTERVAL", "LATERAL", "WINDOW", "FOR",
    ];

    internal static IReadOnlyList<string> DataTypes { get; } =
    [
        "TINYINT", "MEDIUMINT", "DOUBLE", "BIT", "BOOLEAN", "DATETIME", "YEAR", "TEXT", "TINYTEXT", "MEDIUMTEXT", "LONGTEXT",
        "BLOB", "TINYBLOB", "MEDIUMBLOB", "LONGBLOB", "BINARY", "VARBINARY", "ENUM", "JSON",
    ];

    internal static IReadOnlyList<string> Functions { get; } =
    [
        "IFNULL", "IF", "CONCAT", "CONCAT_WS", "GROUP_CONCAT", "NOW", "CURDATE", "CURTIME", "DATE_ADD", "DATE_SUB", "DATEDIFF",
        "DATE_FORMAT", "STR_TO_DATE", "UNIX_TIMESTAMP", "YEAR", "MONTH", "DAY", "LENGTH", "CHAR_LENGTH", "SUBSTR",
        "LAST_INSERT_ID", "UUID", "RAND", "FLOOR", "CEIL", "GREATEST", "LEAST", "JSON_EXTRACT",
    ];

    internal static IReadOnlyList<string> ClauseKeywords { get; } = ["LIMIT", "NATURAL", "USING", "STRAIGHT_JOIN", "WINDOW", "FOR"];
}
