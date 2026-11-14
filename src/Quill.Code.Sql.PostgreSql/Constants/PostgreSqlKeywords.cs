namespace Quill.Code.Sql.PostgreSql.Constants;

/// <summary>PostgreSQL words added to the shared SQL.</summary>
internal static class PostgreSqlKeywords
{
    internal static IReadOnlyList<string> Keywords { get; } =
    [
        "LIMIT", "OFFSET", "FETCH", "FIRST", "NEXT", "ONLY", "RETURNING", "ILIKE", "SIMILAR", "LATERAL", "WINDOW", "FILTER",
        "NULLS", "LAST", "ANY", "SOME", "ARRAY", "INTERVAL", "CONFLICT", "NOTHING", "DO", "LANGUAGE", "RETURNS", "RETURN",
        "DECLARE", "IF", "ELSIF", "LOOP", "WHILE", "FOR", "RAISE", "NOTICE", "EXCEPTION", "PERFORM", "CALL", "EXPLAIN",
        "ANALYZE", "VACUUM", "COPY", "SEQUENCE", "EXTENSION", "MATERIALIZED", "TEMPORARY", "TEMP", "CONCURRENTLY", "CASCADE",
        "RESTRICT", "OWNER", "MERGE",
    ];

    internal static IReadOnlyList<string> DataTypes { get; } =
    [
        "SERIAL", "BIGSERIAL", "SMALLSERIAL", "BOOLEAN", "TEXT", "BYTEA", "UUID", "JSON", "JSONB", "TIMESTAMPTZ", "TIMETZ",
        "INTERVAL", "DOUBLE PRECISION", "MONEY", "INET", "CIDR", "XML", "BIT", "VARBIT",
    ];

    internal static IReadOnlyList<string> Functions { get; } =
    [
        "NOW", "CURRENT_DATE", "CURRENT_TIMESTAMP", "AGE", "DATE_TRUNC", "DATE_PART", "EXTRACT", "TO_CHAR", "TO_DATE",
        "TO_TIMESTAMP", "TO_NUMBER", "LENGTH", "CHAR_LENGTH", "SUBSTR", "POSITION", "SPLIT_PART", "REGEXP_REPLACE", "CONCAT",
        "CONCAT_WS", "STRING_AGG", "ARRAY_AGG", "JSON_AGG", "JSONB_AGG", "UNNEST", "GENERATE_SERIES", "GEN_RANDOM_UUID",
        "RANDOM", "FLOOR", "CEIL", "GREATEST", "LEAST", "NEXTVAL", "CURRVAL",
    ];

    internal static IReadOnlyList<string> ClauseKeywords { get; } =
        ["LIMIT", "FETCH", "RETURNING", "NATURAL", "USING", "LATERAL", "WINDOW", "FOR"];

    internal static IReadOnlyList<string> Phrases { get; } = ["FETCH FIRST 10 ROWS ONLY"];
}
