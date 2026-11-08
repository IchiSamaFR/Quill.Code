using Oracle.ManagedDataAccess.Client;
using System.Data.Common;
using System.Globalization;
using Quill.Data.Sql.Oracle.Constants;

namespace Quill.Data.Sql.Oracle;

/// <summary>
/// Oracle catalog read from the ALL_* views: an Oracle connection holds a single database, whose schemas are the
/// users (excluding accounts supplied by Oracle). The requested database name is therefore not used to connect.
/// </summary>
public sealed class OracleSchemaReader : ISqlSchemaReader
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<SqlDatabaseInfo>> GetDatabasesAsync(string connectionString, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenAsync(connectionString, cancellationToken);
        return [new SqlDatabaseInfo(await GetDatabaseNameAsync(connection, cancellationToken))];
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SqlSchemaObject>> GetObjectsAsync(string connectionString, string database, SqlSchemaObjectKind kind, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(database);

        var query = kind switch
        {
            SqlSchemaObjectKind.Table => OracleQueries.Tables,
            SqlSchemaObjectKind.View => OracleQueries.Views,
            SqlSchemaObjectKind.Procedure => OracleQueries.Procedures,
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
        };

        await using var connection = await OpenAsync(connectionString, cancellationToken);
        await using var command = new OracleCommand(query, connection);
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

        await using var connection = await OpenAsync(connectionString, cancellationToken);
        await using var command = new OracleCommand(OracleQueries.Columns, connection) { BindByName = true };
        command.Parameters.Add(new OracleParameter("owner", schema));
        command.Parameters.Add(new OracleParameter("name", objectName));
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var result = new List<SqlColumn>();
        while (await reader.ReadAsync(cancellationToken))
        {
            var dataType = reader.GetString(1);
            var precision = reader.GetByteValue(4);
            var scale = reader.GetByteValue(5);
            result.Add(new SqlColumn(reader.GetString(0), FormatType(reader, 1))
            {
                SqlType = dataType,
                Precision = precision,
                Scale = scale,
                IsNullable = reader.GetFlag(6),
                IsIdentity = reader.GetFlag(7),
                IsComputed = reader.GetFlag(8),
                PrimaryKeyOrdinal = reader.GetInt32Value(9),
                IsForeignKey = reader.GetFlag(10),
                Comment = reader.GetTrimmedStringOrNull(11),
            });
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<SqlDatabaseSchema> GetSchemaAsync(string connectionString, string? database, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenAsync(connectionString, cancellationToken);
        var databaseName = await GetDatabaseNameAsync(connection, cancellationToken);

        // Oracle ne rend pas plusieurs jeux par commande : deux requetes sur la meme connexion.
        var objects = new List<(string Schema, string Name, SqlSchemaObjectKind Kind)>();
        await using (var command = new OracleCommand(OracleQueries.SnapshotObjects, connection))
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                var kind = reader.GetString(2) switch
                {
                    OracleConstants.TableType => SqlSchemaObjectKind.Table,
                    OracleConstants.ViewType => SqlSchemaObjectKind.View,
                    _ => SqlSchemaObjectKind.Procedure,
                };
                objects.Add((reader.GetString(0), reader.GetString(1), kind));
            }
        }

        var columns = new Dictionary<(string Schema, string Table), List<SqlColumn>>();
        await using (var command = new OracleCommand(OracleQueries.SnapshotColumns, connection))
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                var key = (reader.GetString(0), reader.GetString(1));
                if (!columns.TryGetValue(key, out var list))
                    columns[key] = list = [];

                list.Add(new SqlColumn(reader.GetString(2), FormatType(reader, 3))
                {
                    SqlType = reader.GetString(3),
                    Precision = reader.GetByteValue(6),
                    Scale = reader.GetByteValue(7),
                    IsNullable = reader.GetFlag(8),
                    PrimaryKeyOrdinal = reader.GetInt32Value(9),
                });
            }
        }

        return new SqlDatabaseSchema(
            databaseName,
            objects.Select(o => new SqlSchemaObject(
                o.Schema,
                o.Name,
                o.Kind,
                o.Kind != SqlSchemaObjectKind.Procedure && columns.TryGetValue((o.Schema, o.Name), out var list) ? list : [])).ToArray());
    }

    /// <summary>Type displayed as in SQL Developer; columns read from <paramref name="typeOrdinal"/>: type, bytes, characters, precision, scale.</summary>
    private static string FormatType(DbDataReader reader, int typeOrdinal)
    {
        var typeName = reader.GetString(typeOrdinal);
        var dataLength = reader.GetInt32Value(typeOrdinal + 1);
        var charLength = reader.GetInt32Value(typeOrdinal + 2);
        var hasPrecision = !reader.IsDBNull(typeOrdinal + 3);
        var precision = reader.GetInt32Value(typeOrdinal + 3);
        var scale = reader.GetInt32Value(typeOrdinal + 4);

        return typeName switch
        {
            "VARCHAR2" or "NVARCHAR2" or "CHAR" or "NCHAR" => string.Create(CultureInfo.InvariantCulture, $"{typeName}({charLength})"),
            "RAW" => string.Create(CultureInfo.InvariantCulture, $"{typeName}({dataLength})"),
            "NUMBER" when !hasPrecision => typeName,
            "NUMBER" when scale == 0 => string.Create(CultureInfo.InvariantCulture, $"{typeName}({precision})"),
            "NUMBER" => string.Create(CultureInfo.InvariantCulture, $"{typeName}({precision},{scale})"),
            "FLOAT" when hasPrecision => string.Create(CultureInfo.InvariantCulture, $"{typeName}({precision})"),
            _ => typeName,
        };
    }

    private static async Task<string> GetDatabaseNameAsync(OracleConnection connection, CancellationToken cancellationToken)
    {
        await using var command = new OracleCommand(OracleQueries.Database, connection);
        return Convert.ToString(await command.ExecuteScalarAsync(cancellationToken), CultureInfo.InvariantCulture) ?? connection.DataSource;
    }

    private static async Task<OracleConnection> OpenAsync(string connectionString, CancellationToken cancellationToken)
    {
        var builder = new OracleConnectionStringBuilder(connectionString)
        {
            ConnectionTimeout = OracleConstants.ConnectTimeoutSeconds,
        };

        var connection = new OracleConnection(builder.ConnectionString);
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
