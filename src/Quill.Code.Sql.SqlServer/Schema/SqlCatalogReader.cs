using Quill.Code.Sql.Dialects;
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
    private readonly SqlDialect _dialect;

    public SqlCatalogReader(ISqlCatalog? catalog, SqlDialect dialect)
    {
        _catalog = catalog;
        _dialect = dialect;
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

    public async Task<bool> IsDatabaseAsync(string name, CancellationToken cancellationToken, bool isQuoted = false)
        => (await GetDatabasesAsync(cancellationToken).ConfigureAwait(false))
            .Any(d => _dialect.IdentifierEquals(d.Name, leftQuoted: true, name, isQuoted));

    public SqlSchemaObject? Find(SqlDatabaseSchema database, string? schema, string name, bool nameIsQuoted = false, bool schemaIsQuoted = false)
    {
        var candidates = database.Objects
            .Where(o => _dialect.IdentifierEquals(o.Name, leftQuoted: true, name, nameIsQuoted))
            .Where(o => schema is null || _dialect.IdentifierEquals(o.Schema, leftQuoted: true, schema, schemaIsQuoted))
            .ToArray();

        if (schema is not null)
            return candidates.FirstOrDefault();

        var defaultSchema = _dialect.DefaultSchema;
        return candidates.FirstOrDefault(o => defaultSchema is not null
                && _dialect.IdentifierEquals(o.Schema, leftQuoted: true, defaultSchema, rightQuoted: false))
            ?? candidates.FirstOrDefault();
    }

    public bool HasSchema(SqlDatabaseSchema database, string schema, bool isQuoted = false)
        => database.Objects.Any(o => _dialect.IdentifierEquals(o.Schema, leftQuoted: true, schema, isQuoted));

    /// <summary>Object designated by a table of the batch; null for a temporary or unknown table.</summary>
    public async Task<SqlSchemaObject?> FindAsync(SqlContext scope, SqlTableReference table, CancellationToken cancellationToken)
    {
        if (table.Name.StartsWith('#'))
            return null;

        var database = await GetDatabaseAsync(table.Database ?? scope.Database, cancellationToken).ConfigureAwait(false);
        if (database is not null && Find(database, table.Schema, table.Name, table.NameIsQuoted, table.SchemaIsQuoted) is { } found)
            return found;

        // MySQL, SQLite: "other_db.table", the qualifier is the database.
        if (!_dialect.SchemaIsDatabase || table.Schema is null || table.Database is not null
            || !await IsDatabaseAsync(table.Schema, cancellationToken, table.SchemaIsQuoted).ConfigureAwait(false))
            return null;

        var remote = await GetDatabaseAsync(table.Schema, cancellationToken).ConfigureAwait(false);
        return remote is null ? null : Find(remote, null, table.Name, table.NameIsQuoted);
    }
}
