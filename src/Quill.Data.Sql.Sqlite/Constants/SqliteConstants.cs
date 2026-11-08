namespace Quill.Data.Sql.Sqlite.Constants;

/// <summary>SQLite catalog constants.</summary>
internal static class SqliteConstants
{
    /// <summary>Main database of a connection.</summary>
    internal const string MainDatabase = "main";

    /// <summary>Database of temporary objects.</summary>
    internal const string TempDatabase = "temp";

    internal const string TableType = "table";

    internal const string ViewType = "view";

    /// <summary>Displayed type of a column declared without a type (BLOB affinity, values of any type).</summary>
    internal const string UntypedColumn = "ANY";

    /// <summary>pragma_table_xinfo.hidden: column of a virtual table, never listed.</summary>
    internal const int HiddenColumn = 1;

    /// <summary>pragma_table_xinfo.hidden: from this value on, generated column (VIRTUAL or STORED).</summary>
    internal const int FirstGeneratedColumn = 2;

    /// <summary>Type of a primary key aliasing rowid (auto-incremented).</summary>
    internal const string RowIdAliasType = "INTEGER";
}
