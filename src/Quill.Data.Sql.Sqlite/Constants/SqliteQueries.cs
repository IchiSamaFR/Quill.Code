namespace Quill.Data.Sql.Sqlite.Constants;

/// <summary>
/// SQLite catalog queries (sqlite_master and pragmas as table-valued functions, SQLite 3.16 and later).
/// {0}: quoted database name, since an identifier cannot be passed as a parameter.
/// </summary>
internal static class SqliteQueries
{
    internal const string Databases = """
        SELECT name FROM pragma_database_list ORDER BY seq;
        """;

    internal const string Objects = """
        SELECT name FROM {0}.sqlite_master
        WHERE type = @type AND name NOT LIKE 'sqlite\_%' ESCAPE '\'
        ORDER BY name;
        """;

    internal const string Columns = """
        SELECT x.name, x.type, NOT x."notnull", x.pk, x.hidden,
               EXISTS (SELECT 1 FROM pragma_foreign_key_list(@name, @schema) f WHERE f."from" = x.name)
        FROM pragma_table_xinfo(@name, @schema) x
        ORDER BY x.cid;
        """;

    // One row per column, linked to its table or view; a row without a column for an object that returns none.
    internal const string Snapshot = """
        SELECT m.name, m.type, x.name, x.type, NOT x."notnull", x.pk, x.hidden
        FROM {0}.sqlite_master m
        LEFT JOIN pragma_table_xinfo(m.name, @schema) x
        WHERE m.type IN ('table', 'view') AND m.name NOT LIKE 'sqlite\_%' ESCAPE '\'
        ORDER BY m.name, x.cid;
        """;
}
