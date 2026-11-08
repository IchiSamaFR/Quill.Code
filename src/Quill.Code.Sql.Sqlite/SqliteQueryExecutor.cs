using Microsoft.Data.Sqlite;
using System.Data.Common;
using Quill.Code.Execution;
using Quill.Code.Sql.Execution;

namespace Quill.Code.Sql.Sqlite;

/// <summary>
/// SQLite execution (Microsoft.Data.Sqlite): the script is sent as one command, the driver chains its statements;
/// an error stops the following ones. A single database per file: the requested database is ignored (ATTACH in the script).
/// </summary>
public sealed class SqliteQueryExecutor : DbQueryExecutor
{
    /// <inheritdoc/>
    protected override DbConnection CreateConnection(string connectionString, string? database, bool forRead)
        => new SqliteConnection(new SqliteConnectionStringBuilder(connectionString).ConnectionString);
}
