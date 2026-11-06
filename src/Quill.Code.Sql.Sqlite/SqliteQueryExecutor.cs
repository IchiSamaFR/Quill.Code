using Microsoft.Data.Sqlite;
using System.Data.Common;
using Quill.Code.Execution;
using Quill.Code.Sql.Execution;

namespace Quill.Code.Sql.Sqlite;

/// <summary>
/// Execution SQLite (Microsoft.Data.Sqlite) : le script part en une commande, le pilote enchaine ses instructions ;
/// une erreur arrete les suivantes. Une seule base par fichier : la base demandee est ignoree (ATTACH dans le script).
/// </summary>
public sealed class SqliteQueryExecutor : DbQueryExecutor
{
    /// <inheritdoc/>
    protected override DbConnection CreateConnection(string connectionString, string? database, bool forRead)
        => new SqliteConnection(new SqliteConnectionStringBuilder(connectionString).ConnectionString);
}
