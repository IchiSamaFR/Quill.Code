using MySqlConnector;
using Quill.Data.Sql.MySql.Constants;

namespace Quill.Data.Sql.MySql;

/// <summary>
/// MySQL / MariaDB catalog read from information_schema. A MySQL database is also its only schema:
/// <see cref="SqlSchemaObject.Schema"/> holds the database name.
/// </summary>
public sealed class MySqlSchemaReader : ISqlSchemaReader
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<SqlDatabaseInfo>> GetDatabasesAsync(string connectionString, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenAsync(connectionString, database: null, cancellationToken);
        await using var command = new MySqlCommand(MySqlQueries.Databases, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var result = new List<SqlDatabaseInfo>();
        while (await reader.ReadAsync(cancellationToken))
        {
            var name = reader.GetString(0);
            result.Add(new SqlDatabaseInfo(name, MySqlConstants.SystemDatabases.Contains(name)));
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SqlSchemaObject>> GetObjectsAsync(string connectionString, string database, SqlSchemaObjectKind kind, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(database);

        await using var connection = await OpenAsync(connectionString, database, cancellationToken);
        await using var command = new MySqlCommand(kind == SqlSchemaObjectKind.Procedure ? MySqlQueries.Procedures : MySqlQueries.Tables, connection);
        command.Parameters.AddWithValue("@schema", database);
        if (kind != SqlSchemaObjectKind.Procedure)
            command.Parameters.AddWithValue("@type", kind == SqlSchemaObjectKind.View ? MySqlConstants.ViewType : MySqlConstants.TableType);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var result = new List<SqlSchemaObject>();
        while (await reader.ReadAsync(cancellationToken))
            result.Add(new SqlSchemaObject(reader.GetString(0), reader.GetString(1), kind, []) { Comment = reader.GetTrimmedStringOrNull(2) });

        return result;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SqlColumn>> GetColumnsAsync(string connectionString, string database, string schema, string objectName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(database);

        await using var connection = await OpenAsync(connectionString, database, cancellationToken);
        await using var command = new MySqlCommand(MySqlQueries.Columns, connection);
        command.Parameters.AddWithValue("@schema", string.IsNullOrWhiteSpace(schema) ? database : schema);
        command.Parameters.AddWithValue("@name", objectName);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var result = new List<SqlColumn>();
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(new SqlColumn(reader.GetString(0), reader.GetString(1))
            {
                SqlType = reader.GetString(2),
                Precision = reader.GetByteValue(3),
                Scale = reader.GetByteValue(4),
                IsNullable = reader.GetFlag(5),
                IsIdentity = reader.GetFlag(6),
                IsComputed = reader.GetFlag(7),
                PrimaryKeyOrdinal = reader.GetInt32Value(8),
                IsForeignKey = reader.GetFlag(9),
                Comment = reader.GetTrimmedStringOrNull(10),
            });
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<SqlDatabaseSchema> GetSchemaAsync(string connectionString, string? database, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenAsync(connectionString, database, cancellationToken);
        await using var command = new MySqlCommand(MySqlQueries.Snapshot, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        await reader.ReadAsync(cancellationToken);
        var databaseName = reader.IsDBNull(0)
            ? throw new InvalidOperationException("No database selected: specify Database in the connection string.")
            : reader.GetString(0);

        await reader.NextResultAsync(cancellationToken);
        var objects = new List<(string Name, SqlSchemaObjectKind Kind)>();
        while (await reader.ReadAsync(cancellationToken))
            objects.Add((reader.GetString(0), reader.GetString(1) == MySqlConstants.ViewType ? SqlSchemaObjectKind.View : SqlSchemaObjectKind.Table));

        await reader.NextResultAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            objects.Add((reader.GetString(0), SqlSchemaObjectKind.Procedure));

        await reader.NextResultAsync(cancellationToken);
        var columns = new Dictionary<string, List<SqlColumn>>(StringComparer.OrdinalIgnoreCase);
        while (await reader.ReadAsync(cancellationToken))
        {
            var table = reader.GetString(0);
            if (!columns.TryGetValue(table, out var list))
                columns[table] = list = [];

            list.Add(new SqlColumn(reader.GetString(1), reader.GetString(2))
            {
                SqlType = reader.GetString(3),
                Precision = reader.GetByteValue(4),
                Scale = reader.GetByteValue(5),
                IsNullable = reader.GetFlag(6),
                PrimaryKeyOrdinal = reader.GetInt32Value(7),
            });
        }

        return new SqlDatabaseSchema(
            databaseName,
            objects.Select(o => new SqlSchemaObject(
                databaseName,
                o.Name,
                o.Kind,
                o.Kind != SqlSchemaObjectKind.Procedure && columns.TryGetValue(o.Name, out var list) ? list : [])).ToArray());
    }

    private static async Task<MySqlConnection> OpenAsync(string connectionString, string? database, CancellationToken cancellationToken)
    {
        var builder = new MySqlConnectionStringBuilder(connectionString)
        {
            ConnectionTimeout = MySqlConstants.ConnectTimeoutSeconds,
        };
        if (database is not null)
            builder.Database = database;

        var connection = new MySqlConnection(builder.ConnectionString);
        try
        {
            await connection.OpenAsync(cancellationToken);
            return connection;
        }
        catch
        {
            await connection.DisposeAsync();
            throw;
        }
    }
}
