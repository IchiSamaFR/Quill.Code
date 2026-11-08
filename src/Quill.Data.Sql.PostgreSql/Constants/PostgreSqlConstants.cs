namespace Quill.Data.Sql.PostgreSql.Constants;

/// <summary>PostgreSQL connection and catalog constants.</summary>
internal static class PostgreSqlConstants
{
    internal const int ConnectTimeoutSeconds = 8;

    /// <summary>pg_class.relkind of a view or materialized view.</summary>
    internal const string ViewKinds = "vm";
}
