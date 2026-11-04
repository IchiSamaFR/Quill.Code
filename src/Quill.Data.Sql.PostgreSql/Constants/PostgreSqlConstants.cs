namespace Quill.Data.Sql.PostgreSql.Constants;

/// <summary>Constantes de connexion et du catalogue PostgreSQL.</summary>
internal static class PostgreSqlConstants
{
    internal const int ConnectTimeoutSeconds = 8;

    /// <summary>pg_class.relkind d'une vue ou d'une vue materialisee.</summary>
    internal const string ViewKinds = "vm";
}
