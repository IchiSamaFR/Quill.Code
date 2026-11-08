namespace Quill.Data.Sql.PostgreSql.Constants;

/// <summary>Catalog queries on pg_catalog (PostgreSQL 12 and later).</summary>
internal static class PostgreSqlQueries
{
    internal const string Databases = """
        SELECT datname FROM pg_database WHERE datallowconn AND NOT datistemplate ORDER BY datname;
        """;

    // Schemas du serveur (catalogue, TOAST, tables temporaires) exclus de toutes les lectures.
    private const string UserSchemas = """
        n.nspname NOT IN ('pg_catalog', 'information_schema') AND n.nspname NOT LIKE 'pg\_toast%' AND n.nspname NOT LIKE 'pg\_temp\_%'
        """;

    // r : table, p : table partitionnee (ses partitions sont masquees) ; v : vue, m : vue materialisee.
    internal const string Tables = $"""
        SELECT n.nspname, c.relname, obj_description(c.oid, 'pg_class')
        FROM pg_class c
        INNER JOIN pg_namespace n ON n.oid = c.relnamespace
        WHERE c.relkind IN ('r', 'p') AND NOT c.relispartition AND {UserSchemas}
        ORDER BY n.nspname, c.relname;
        """;

    internal const string Views = $"""
        SELECT n.nspname, c.relname, obj_description(c.oid, 'pg_class')
        FROM pg_class c
        INNER JOIN pg_namespace n ON n.oid = c.relnamespace
        WHERE c.relkind IN ('v', 'm') AND {UserSchemas}
        ORDER BY n.nspname, c.relname;
        """;

    // Procedures et fonctions (hors agregats) ; une surcharge = un seul nom.
    internal const string Procedures = $"""
        SELECT n.nspname, p.proname, MAX(obj_description(p.oid, 'pg_proc'))
        FROM pg_proc p
        INNER JOIN pg_namespace n ON n.oid = p.pronamespace
        WHERE p.prokind IN ('p', 'f') AND {UserSchemas}
        GROUP BY n.nspname, p.proname
        ORDER BY n.nspname, p.proname;
        """;

    // Memes calculs de type, precision et echelle que information_schema.columns ; domaine : type de base.
    private const string ColumnTypes = """
        format_type(a.atttypid, a.atttypmod),
        COALESCE(bt.typname, t.typname),
        COALESCE(information_schema._pg_numeric_precision(information_schema._pg_truetypid(a.*, t.*), information_schema._pg_truetypmod(a.*, t.*)), 0),
        COALESCE(information_schema._pg_numeric_scale(information_schema._pg_truetypid(a.*, t.*), information_schema._pg_truetypmod(a.*, t.*)),
                 information_schema._pg_datetime_precision(information_schema._pg_truetypid(a.*, t.*), information_schema._pg_truetypmod(a.*, t.*)), 0),
        NOT a.attnotnull,
        COALESCE((SELECT array_position(i.indkey::int2[], a.attnum) FROM pg_index i WHERE i.indrelid = a.attrelid AND i.indisprimary), 0)
        """;

    private const string ColumnSources = """
        FROM pg_attribute a
        INNER JOIN pg_class c ON c.oid = a.attrelid
        INNER JOIN pg_namespace n ON n.oid = c.relnamespace
        INNER JOIN pg_type t ON t.oid = a.atttypid
        LEFT JOIN pg_type bt ON t.typtype = 'd' AND bt.oid = t.typbasetype
        """;

    internal const string Columns = $"""
        SELECT a.attname, {ColumnTypes},
               a.attidentity <> '' OR COALESCE(pg_get_expr(d.adbin, d.adrelid) LIKE 'nextval(%', false),
               a.attgenerated <> '',
               EXISTS (SELECT 1 FROM pg_constraint fk WHERE fk.conrelid = a.attrelid AND fk.contype = 'f' AND a.attnum = ANY (fk.conkey)),
               col_description(a.attrelid, a.attnum)
        {ColumnSources}
        LEFT JOIN pg_attrdef d ON d.adrelid = a.attrelid AND d.adnum = a.attnum
        WHERE n.nspname = @schema AND c.relname = @name AND a.attnum > 0 AND NOT a.attisdropped
        ORDER BY a.attnum;
        """;

    // Quatre jeux : base courante, tables et vues (rattachees par oid), procedures, colonnes.
    internal const string Snapshot = $"""
        SELECT current_database();

        SELECT c.oid::bigint, n.nspname, c.relname, c.relkind::text
        FROM pg_class c
        INNER JOIN pg_namespace n ON n.oid = c.relnamespace
        WHERE ((c.relkind IN ('r', 'p') AND NOT c.relispartition) OR c.relkind IN ('v', 'm')) AND {UserSchemas};

        SELECT DISTINCT n.nspname, p.proname
        FROM pg_proc p
        INNER JOIN pg_namespace n ON n.oid = p.pronamespace
        WHERE p.prokind IN ('p', 'f') AND {UserSchemas};

        SELECT a.attrelid::bigint, a.attname, {ColumnTypes}
        {ColumnSources}
        WHERE ((c.relkind IN ('r', 'p') AND NOT c.relispartition) OR c.relkind IN ('v', 'm')) AND {UserSchemas}
          AND a.attnum > 0 AND NOT a.attisdropped
        ORDER BY a.attrelid, a.attnum;
        """;
}
