using Quill.Code.SqlServer.Completion;
using Quill.Data.Sql;

namespace Quill.Code.SqlServer.Schema;

/// <summary>
/// Accès au catalogue de l'hôte pour l'autocomplétion et le survol : une erreur du catalogue (serveur injoignable…)
/// vaut absence de schéma, jamais une exception à la frappe ou au survol.
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

    /// <summary>Objet désigné par une table du lot ; null pour une table temporaire ou inconnue.</summary>
    public async Task<SqlSchemaObject?> FindAsync(SqlContext scope, SqlTableReference table, CancellationToken cancellationToken)
    {
        if (table.Name.StartsWith('#'))
            return null;

        var database = await GetDatabaseAsync(table.Database ?? scope.Database, cancellationToken).ConfigureAwait(false);
        return database?.Find(table.Schema, table.Name);
    }
}
