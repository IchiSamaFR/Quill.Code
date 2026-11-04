namespace Quill.Data.Sql.Sqlite.Constants;

/// <summary>Constantes du catalogue SQLite.</summary>
internal static class SqliteConstants
{
    /// <summary>Base principale d'une connexion.</summary>
    internal const string MainDatabase = "main";

    /// <summary>Base des objets temporaires.</summary>
    internal const string TempDatabase = "temp";

    internal const string TableType = "table";

    internal const string ViewType = "view";

    /// <summary>Type affiche d'une colonne declaree sans type (affinite BLOB, valeurs de tout type).</summary>
    internal const string UntypedColumn = "ANY";

    /// <summary>pragma_table_xinfo.hidden : colonne d'une table virtuelle, jamais listee.</summary>
    internal const int HiddenColumn = 1;

    /// <summary>pragma_table_xinfo.hidden : a partir de cette valeur, colonne generee (VIRTUAL ou STORED).</summary>
    internal const int FirstGeneratedColumn = 2;

    /// <summary>Type d'une cle primaire alias de rowid (auto-incrementee).</summary>
    internal const string RowIdAliasType = "INTEGER";
}
