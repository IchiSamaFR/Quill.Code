using Quill.Data.Sql;

namespace Quill.Code.Sql.SqlServer.Schema;

/// <summary>
/// Catalog of databases, provided by the host: Quill connects to nothing. An unavailable database (closed connection,
/// insufficient permissions…) is returned empty or null rather than through an exception; Quill treats any
/// exception other than a cancellation as a missing schema anyway.
/// </summary>
public interface ISqlCatalog
{
    /// <summary>Databases of the server (USE, database.schema.object).</summary>
    Task<IReadOnlyList<SqlDatabaseInfo>> GetDatabasesAsync(CancellationToken cancellationToken = default);

    /// <summary>Complete schema of a database, columns included; null if it is unknown or unavailable.</summary>
    /// <param name="database">Name of the database; null = default database of the connection.</param>
    Task<SqlDatabaseSchema?> GetDatabaseAsync(string? database, CancellationToken cancellationToken = default);
}
