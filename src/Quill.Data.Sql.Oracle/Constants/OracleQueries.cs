namespace Quill.Data.Sql.Oracle.Constants;

/// <summary>Requêtes de lecture du catalogue dans les vues ALL_* (Oracle 12c et suivants).</summary>
internal static class OracleQueries
{
    internal const string Database = """
        SELECT SYS_CONTEXT('USERENV', 'DB_NAME') FROM DUAL
        """;

    // Schemas des utilisateurs, hors comptes fournis par Oracle (SYS, SYSTEM, XDB...).
    private const string UserOwners = """
        (SELECT USERNAME FROM ALL_USERS WHERE ORACLE_MAINTAINED = 'N')
        """;

    internal const string Tables = $"""
        SELECT t.OWNER, t.TABLE_NAME, m.COMMENTS
        FROM ALL_TABLES t
        LEFT JOIN ALL_TAB_COMMENTS m ON m.OWNER = t.OWNER AND m.TABLE_NAME = t.TABLE_NAME
        WHERE t.OWNER IN {UserOwners} AND t.DROPPED = 'NO' AND t.NESTED = 'NO'
        ORDER BY t.OWNER, t.TABLE_NAME
        """;

    internal const string Views = $"""
        SELECT v.OWNER, v.VIEW_NAME, m.COMMENTS
        FROM ALL_VIEWS v
        LEFT JOIN ALL_TAB_COMMENTS m ON m.OWNER = v.OWNER AND m.TABLE_NAME = v.VIEW_NAME
        WHERE v.OWNER IN {UserOwners}
        ORDER BY v.OWNER, v.VIEW_NAME
        """;

    // Procedures et fonctions autonomes ; celles des packages restent dans leur package.
    internal const string Procedures = $"""
        SELECT OWNER, OBJECT_NAME, NULL
        FROM ALL_OBJECTS
        WHERE OBJECT_TYPE IN ('PROCEDURE', 'FUNCTION') AND OWNER IN {UserOwners}
        ORDER BY OWNER, OBJECT_NAME
        """;

    private const string PrimaryKeyColumns = """
        SELECT cc.OWNER, cc.TABLE_NAME, cc.COLUMN_NAME, cc.POSITION
        FROM ALL_CONSTRAINTS k
        INNER JOIN ALL_CONS_COLUMNS cc ON cc.OWNER = k.OWNER AND cc.CONSTRAINT_NAME = k.CONSTRAINT_NAME
        WHERE k.CONSTRAINT_TYPE = 'P'
        """;

    // DATA_TYPE porte deja la precision des types temporels (TIMESTAMP(6)) ; longueur et precision mises en forme par le lecteur.
    internal const string Columns = $"""
        SELECT c.COLUMN_NAME, c.DATA_TYPE, c.DATA_LENGTH, c.CHAR_LENGTH, c.DATA_PRECISION, c.DATA_SCALE,
               CASE WHEN c.NULLABLE = 'Y' THEN 1 ELSE 0 END,
               CASE WHEN c.IDENTITY_COLUMN = 'YES' THEN 1 ELSE 0 END,
               CASE WHEN c.VIRTUAL_COLUMN = 'YES' THEN 1 ELSE 0 END,
               NVL(pk.POSITION, 0),
               CASE WHEN EXISTS (
                   SELECT 1 FROM ALL_CONSTRAINTS k
                   INNER JOIN ALL_CONS_COLUMNS cc ON cc.OWNER = k.OWNER AND cc.CONSTRAINT_NAME = k.CONSTRAINT_NAME
                   WHERE k.CONSTRAINT_TYPE = 'R' AND cc.OWNER = c.OWNER AND cc.TABLE_NAME = c.TABLE_NAME AND cc.COLUMN_NAME = c.COLUMN_NAME)
                   THEN 1 ELSE 0 END,
               m.COMMENTS
        FROM ALL_TAB_COLS c
        LEFT JOIN ({PrimaryKeyColumns}) pk ON pk.OWNER = c.OWNER AND pk.TABLE_NAME = c.TABLE_NAME AND pk.COLUMN_NAME = c.COLUMN_NAME
        LEFT JOIN ALL_COL_COMMENTS m ON m.OWNER = c.OWNER AND m.TABLE_NAME = c.TABLE_NAME AND m.COLUMN_NAME = c.COLUMN_NAME
        WHERE c.OWNER = :owner AND c.TABLE_NAME = :name AND c.HIDDEN_COLUMN = 'NO'
        ORDER BY c.COLUMN_ID
        """;

    internal const string SnapshotObjects = $"""
        SELECT OWNER, OBJECT_NAME, OBJECT_TYPE
        FROM ALL_OBJECTS
        WHERE OBJECT_TYPE IN ('TABLE', 'VIEW', 'PROCEDURE', 'FUNCTION') AND OWNER IN {UserOwners}
          AND OBJECT_NAME NOT LIKE 'BIN$%'
        """;

    internal const string SnapshotColumns = $"""
        SELECT c.OWNER, c.TABLE_NAME, c.COLUMN_NAME, c.DATA_TYPE, c.DATA_LENGTH, c.CHAR_LENGTH, c.DATA_PRECISION, c.DATA_SCALE,
               CASE WHEN c.NULLABLE = 'Y' THEN 1 ELSE 0 END,
               NVL(pk.POSITION, 0)
        FROM ALL_TAB_COLS c
        LEFT JOIN ({PrimaryKeyColumns}) pk ON pk.OWNER = c.OWNER AND pk.TABLE_NAME = c.TABLE_NAME AND pk.COLUMN_NAME = c.COLUMN_NAME
        WHERE c.OWNER IN {UserOwners} AND c.HIDDEN_COLUMN = 'NO'
        ORDER BY c.OWNER, c.TABLE_NAME, c.COLUMN_ID
        """;
}
