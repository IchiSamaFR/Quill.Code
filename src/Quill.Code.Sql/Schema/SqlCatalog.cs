using Quill.Data.Sql;

namespace Quill.Code.Sql.Schema;

/// <summary>Catalogue en mémoire : schémas déjà chargés, ou construits pour des tests.</summary>
public sealed class SqlCatalog : ISqlCatalog
{
    private readonly Dictionary<string, SqlDatabaseSchema> _databases;
    private readonly SqlDatabaseSchema? _default;
    private readonly IReadOnlyList<SqlDatabaseInfo> _infos;

    /// <param name="databases">Bases connues ; la première est la base par défaut.</param>
    public SqlCatalog(IEnumerable<SqlDatabaseSchema> databases)
    {
        ArgumentNullException.ThrowIfNull(databases);

        var all = databases.ToArray();
        _default = all.FirstOrDefault();
        _databases = all.ToDictionary(d => d.Name, StringComparer.OrdinalIgnoreCase);
        _infos = all.Select(d => new SqlDatabaseInfo(d.Name)).ToArray();
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<SqlDatabaseInfo>> GetDatabasesAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(_infos);

    /// <inheritdoc />
    public Task<SqlDatabaseSchema?> GetDatabaseAsync(string? database, CancellationToken cancellationToken = default)
        => Task.FromResult(database is null ? _default : _databases.GetValueOrDefault(database));
}
