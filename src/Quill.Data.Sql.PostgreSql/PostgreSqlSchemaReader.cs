using Npgsql;
using Quill.Data.Sql.PostgreSql.Constants;

namespace Quill.Data.Sql.PostgreSql;

/// <summary>PostgreSQL catalog read from pg_catalog; functions are listed with procedures.</summary>
public sealed class PostgreSqlSchemaReader : ISqlSchemaReader
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<SqlDatabaseInfo>> GetDatabasesAsync(string connectionString, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenAsync(connectionString, database: null, cancellationToken);
        await using var command = new NpgsqlCommand(PostgreSqlQueries.Databases, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var result = new List<SqlDatabaseInfo>();
        while (await reader.ReadAsync(cancellationToken))
            result.Add(new SqlDatabaseInfo(reader.GetString(0)));

        return result;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SqlSchemaObject>> GetObjectsAsync(string connectionString, string database, SqlSchemaObjectKind kind, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(database);

        var query = kind switch
        {
            SqlSchemaObjectKind.Table => PostgreSqlQueries.Tables,
            SqlSchemaObjectKind.View => PostgreSqlQueries.Views,
            SqlSchemaObjectKind.Procedure => PostgreSqlQueries.Procedures,
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
        };

        await using var connection = await OpenAsync(connectionString, database, cancellationToken);
        await using var command = new NpgsqlCommand(query, connection);
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
        await using var command = new NpgsqlCommand(PostgreSqlQueries.Columns, connection);
        command.Parameters.AddWithValue("schema", schema);
        command.Parameters.AddWithValue("name", objectName);
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
                PrimaryKeyOrdinal = reader.GetInt32Value(6),
                IsIdentity = reader.GetFlag(7),
                IsComputed = reader.GetFlag(8),
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
        await using var command = new NpgsqlCommand(PostgreSqlQueries.Snapshot, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        await reader.ReadAsync(cancellationToken);
        var databaseName = reader.GetString(0);

        await reader.NextResultAsync(cancellationToken);
        var objects = new List<(long Id, string Schema, string Name, SqlSchemaObjectKind Kind)>();
        while (await reader.ReadAsync(cancellationToken))
        {
            var kind = PostgreSqlConstants.ViewKinds.Contains(reader.GetString(3), StringComparison.Ordinal) ? SqlSchemaObjectKind.View : SqlSchemaObjectKind.Table;
            objects.Add((reader.GetInt64(0), reader.GetString(1), reader.GetString(2), kind));
        }

        await reader.NextResultAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            objects.Add((0, reader.GetString(0), reader.GetString(1), SqlSchemaObjectKind.Procedure));

        await reader.NextResultAsync(cancellationToken);
        var columns = new Dictionary<long, List<SqlColumn>>();
        while (await reader.ReadAsync(cancellationToken))
        {
            var objectId = reader.GetInt64(0);
            if (!columns.TryGetValue(objectId, out var list))
                columns[objectId] = list = [];

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
            objects.Select(o => new SqlSchemaObject(o.Schema, o.Name, o.Kind, columns.TryGetValue(o.Id, out var list) ? list : [])).ToArray());
    }

    private static async Task<NpgsqlConnection> OpenAsync(string connectionString, string? database, CancellationToken cancellationToken)
    {
        var builder = new NpgsqlConnectionStringBuilder(connectionString)
        {
            Timeout = PostgreSqlConstants.ConnectTimeoutSeconds,
        };
        if (database is not null)
            builder.Database = database;

        var connection = new NpgsqlConnection(builder.ConnectionString);
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
