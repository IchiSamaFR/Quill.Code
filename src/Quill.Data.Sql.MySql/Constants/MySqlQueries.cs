namespace Quill.Data.Sql.MySql.Constants;

/// <summary>Requêtes de lecture du catalogue dans information_schema (MySQL 8, MariaDB 10.3 et suivants).</summary>
internal static class MySqlQueries
{
    internal const string Databases = """
        SELECT SCHEMA_NAME FROM information_schema.SCHEMATA ORDER BY SCHEMA_NAME;
        """;

    // TABLE_COMMENT vaut « VIEW » pour une vue : ce n'est pas un commentaire.
    internal const string Tables = """
        SELECT TABLE_SCHEMA, TABLE_NAME,
               CASE WHEN TABLE_TYPE = 'VIEW' THEN NULL ELSE NULLIF(TABLE_COMMENT, '') END
        FROM information_schema.TABLES
        WHERE TABLE_SCHEMA = @schema AND TABLE_TYPE = @type
        ORDER BY TABLE_NAME;
        """;

    internal const string Procedures = """
        SELECT ROUTINE_SCHEMA, ROUTINE_NAME, NULLIF(ROUTINE_COMMENT, '')
        FROM information_schema.ROUTINES
        WHERE ROUTINE_SCHEMA = @schema AND ROUTINE_TYPE = 'PROCEDURE'
        ORDER BY ROUTINE_NAME;
        """;

    // COLUMN_TYPE est le type affiche (int unsigned, varchar(50)) ; DATA_TYPE le type de base.
    internal const string Columns = """
        SELECT c.COLUMN_NAME, c.COLUMN_TYPE, c.DATA_TYPE,
               COALESCE(c.NUMERIC_PRECISION, 0), COALESCE(c.NUMERIC_SCALE, c.DATETIME_PRECISION, 0),
               c.IS_NULLABLE = 'YES', c.EXTRA LIKE '%auto_increment%', c.EXTRA LIKE '%GENERATED%',
               COALESCE((SELECT k.ORDINAL_POSITION FROM information_schema.KEY_COLUMN_USAGE k
                   WHERE k.TABLE_SCHEMA = c.TABLE_SCHEMA AND k.TABLE_NAME = c.TABLE_NAME
                     AND k.COLUMN_NAME = c.COLUMN_NAME AND k.CONSTRAINT_NAME = 'PRIMARY'), 0),
               EXISTS (SELECT 1 FROM information_schema.KEY_COLUMN_USAGE k
                   WHERE k.TABLE_SCHEMA = c.TABLE_SCHEMA AND k.TABLE_NAME = c.TABLE_NAME
                     AND k.COLUMN_NAME = c.COLUMN_NAME AND k.REFERENCED_TABLE_NAME IS NOT NULL),
               NULLIF(c.COLUMN_COMMENT, '')
        FROM information_schema.COLUMNS c
        WHERE c.TABLE_SCHEMA = @schema AND c.TABLE_NAME = @name
        ORDER BY c.ORDINAL_POSITION;
        """;

    // Quatre jeux : base courante, tables et vues, procedures, colonnes (rattachees par nom de table).
    internal const string Snapshot = """
        SELECT DATABASE();

        SELECT TABLE_NAME, TABLE_TYPE
        FROM information_schema.TABLES
        WHERE TABLE_SCHEMA = DATABASE() AND TABLE_TYPE IN ('BASE TABLE', 'VIEW');

        SELECT ROUTINE_NAME
        FROM information_schema.ROUTINES
        WHERE ROUTINE_SCHEMA = DATABASE() AND ROUTINE_TYPE = 'PROCEDURE';

        SELECT c.TABLE_NAME, c.COLUMN_NAME, c.COLUMN_TYPE, c.DATA_TYPE,
               COALESCE(c.NUMERIC_PRECISION, 0), COALESCE(c.NUMERIC_SCALE, c.DATETIME_PRECISION, 0),
               c.IS_NULLABLE = 'YES', COALESCE(k.ORDINAL_POSITION, 0)
        FROM information_schema.COLUMNS c
        LEFT JOIN information_schema.KEY_COLUMN_USAGE k
               ON k.TABLE_SCHEMA = c.TABLE_SCHEMA AND k.TABLE_NAME = c.TABLE_NAME
              AND k.COLUMN_NAME = c.COLUMN_NAME AND k.CONSTRAINT_NAME = 'PRIMARY'
        WHERE c.TABLE_SCHEMA = DATABASE()
        ORDER BY c.TABLE_NAME, c.ORDINAL_POSITION;
        """;
}
