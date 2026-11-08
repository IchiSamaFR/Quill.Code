namespace Quill.Data.Sql;

/// <summary>Reads the catalog of a server; one implementation per engine (e.g. Quill.Data.Sql.SqlServer).</summary>
public interface ISqlSchemaReader
{
    /// <summary>Online databases of the server, sorted by name.</summary>
    Task<IReadOnlyList<SqlDatabaseInfo>> GetDatabasesAsync(string connectionString, CancellationToken cancellationToken = default);

    /// <summary>User objects of a given kind, without their columns, sorted by schema then name.</summary>
    Task<IReadOnlyList<SqlSchemaObject>> GetObjectsAsync(string connectionString, string database, SqlSchemaObjectKind kind, CancellationToken cancellationToken = default);

    /// <summary>Columns of a table or view, in declaration order.</summary>
    Task<IReadOnlyList<SqlColumn>> GetColumnsAsync(string connectionString, string database, string schema, string objectName, CancellationToken cancellationToken = default);

    /// <summary>Tables, views, procedures and columns of a database in a single round trip.</summary>
    /// <param name="database">Name of the database; null = database of the connection string.</param>
    Task<SqlDatabaseSchema> GetSchemaAsync(string connectionString, string? database, CancellationToken cancellationToken = default);
}
