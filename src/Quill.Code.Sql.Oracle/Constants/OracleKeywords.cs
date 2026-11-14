namespace Quill.Code.Sql.Oracle.Constants;

/// <summary>Oracle words (SQL and PL/SQL) added to the shared SQL.</summary>
internal static class OracleKeywords
{
    internal static IReadOnlyList<string> Keywords { get; } =
    [
        "FETCH", "FIRST", "NEXT", "ONLY", "OFFSET", "PERCENT", "TIES", "ROWNUM", "ROWID", "MINUS", "CONNECT", "START", "PRIOR",
        "NOCYCLE", "LEVEL", "SIBLINGS", "DUAL", "RETURNING", "MERGE", "MATCHED", "PIVOT", "UNPIVOT", "NULLS", "LAST", "WITHIN",
        "DECLARE", "EXCEPTION", "IF", "ELSIF", "LOOP", "WHILE", "FOR", "EXIT", "RETURN", "CURSOR", "OPEN", "CLOSE", "TYPE",
        "RECORD", "PACKAGE", "BODY", "SEQUENCE", "SYNONYM", "MATERIALIZED", "PRAGMA", "EXECUTE", "IMMEDIATE", "BULK", "COLLECT",
        "FORALL", "CALL", "PURGE", "FLASHBACK", "CASCADE", "CONSTRAINTS", "COMMENT", "TABLESPACE", "NOWAIT", "LOCK",
    ];

    internal static IReadOnlyList<string> DataTypes { get; } =
    [
        "NUMBER", "VARCHAR2", "NVARCHAR2", "NCHAR", "CLOB", "NCLOB", "BLOB", "RAW", "LONG", "BINARY_FLOAT", "BINARY_DOUBLE",
        "BINARY_INTEGER", "PLS_INTEGER", "BOOLEAN", "XMLTYPE", "INTERVAL",
    ];

    internal static IReadOnlyList<string> Functions { get; } =
    [
        "NVL", "NVL2", "DECODE", "TO_CHAR", "TO_DATE", "TO_NUMBER", "TO_TIMESTAMP", "SYSDATE", "SYSTIMESTAMP", "CURRENT_DATE",
        "CURRENT_TIMESTAMP", "TRUNC", "ADD_MONTHS", "MONTHS_BETWEEN", "LAST_DAY", "EXTRACT", "LENGTH", "SUBSTR", "INSTR",
        "CONCAT", "LISTAGG", "INITCAP", "LPAD", "RPAD", "REGEXP_LIKE", "REGEXP_REPLACE", "REGEXP_SUBSTR", "SYS_GUID", "MOD",
        "CEIL", "FLOOR", "GREATEST", "LEAST", "USER",
    ];

    internal static IReadOnlyList<string> ClauseKeywords { get; } =
        ["FETCH", "MINUS", "CONNECT", "START", "RETURNING", "PIVOT", "UNPIVOT", "NATURAL", "USING", "FOR"];

    internal static IReadOnlyList<string> Phrases { get; } =
        ["FETCH FIRST 10 ROWS ONLY", "OFFSET 0 ROWS FETCH NEXT 10 ROWS ONLY"];
}
