using System.Collections.Frozen;

namespace Quill.Data.Sql.MySql.Constants;

/// <summary>Constantes de connexion et du catalogue MySQL.</summary>
internal static class MySqlConstants
{
    internal const int ConnectTimeoutSeconds = 8;

    /// <summary>Bases du serveur lui-meme.</summary>
    internal static readonly FrozenSet<string> SystemDatabases = new[] { "mysql", "information_schema", "performance_schema", "sys" }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    internal const string TableType = "BASE TABLE";

    internal const string ViewType = "VIEW";
}
