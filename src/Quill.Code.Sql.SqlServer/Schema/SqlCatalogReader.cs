using Quill.Code.Sql.SqlServer.Completion;
using Quill.Data.Sql;

namespace Quill.Code.Sql.SqlServer.Schema;

/// <summary>
/// Access to the host catalog for completion and hover: a catalog error (unreachable server…)
/// counts as a missing schema, never an exception while typing or hovering.
/// </summary>
internal sealed class SqlCatalogReader
{
    private readonly ISqlCatalog? _catalog;

    public SqlCatalogReader(ISqlCatalog? catalog)
    {
        _catalog = catalog;
    }

    public bool IsAvailable => _catalog is not null;

    public async Task<SqlDatabaseSchema?> GetDatabaseAsync(string? database, CancellationToken cancellationToken)
    {
        if (_catalog is null)
            return null;

        try
        {
            return await _catalog.GetDatabaseAsync(string.IsNullOrWhiteSpace(database) ? null : database, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return null;
        }
    }

    public async Task<IReadOnlyList<SqlDatabaseInfo>> GetDatabasesAsync(CancellationToken cancellationToken)
    {
        if (_catalog is null)
            return [];

        try
        {
            return await _catalog.GetDatabasesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return [];
        }
    }

    public async Task<bool> IsDatabaseAsync(string name, CancellationToken cancellationToken)
        => (await GetDatabasesAsync(cancellationToken).ConfigureAwait(false))
            .Any(d => string.Equals(d.Name, name, StringComparison.OrdinalIgnoreCase));

    /// <summary>Object designated by a table of the batch; null for a temporary or unknown table.</summary>
    public async Task<SqlSchemaObject?> FindAsync(SqlContext scope, SqlTableReference table, CancellationToken cancellationToken)
    {
        if (table.Name.StartsWith('#'))
            return null;

        var database = await GetDatabaseAsync(table.Database ?? scope.Database, cancellationToken).ConfigureAwait(false);
        return database?.Find(table.Schema, table.Name);
    }
}
