using System.Collections.Frozen;

namespace Quill.Data.Sql.MySql.Constants;

/// <summary>MySQL connection and catalog constants.</summary>
internal static class MySqlConstants
{
    internal const int ConnectTimeoutSeconds = 8;

    /// <summary>The server's own databases.</summary>
    internal static readonly FrozenSet<string> SystemDatabases = new[] { "mysql", "information_schema", "performance_schema", "sys" }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    internal const string TableType = "BASE TABLE";

    internal const string ViewType = "VIEW";
}
