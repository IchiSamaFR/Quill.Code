using Microsoft.Data.SqlClient;
using System.Globalization;
using Quill.Data.Sql.SqlServer.Constants;

namespace Quill.Data.Sql.SqlServer;

/// <summary>Catalogue SQL Server lu dans les vues systeme (sys.databases, sys.objects).</summary>
public sealed class SqlServerSchemaReader : ISqlSchemaReader
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<SqlDatabaseInfo>> GetDatabasesAsync(string connectionString, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenAsync(connectionString, database: null, cancellationToken);
        await using var command = new SqlCommand(SqlServerQueries.Databases, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var result = new List<SqlDatabaseInfo>();
        while (await reader.ReadAsync(cancellationToken))
            result.Add(new SqlDatabaseInfo(reader.GetString(0), reader.GetBoolean(1)));

        return result;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SqlSchemaObject>> GetObjectsAsync(string connectionString, string database, SqlSchemaObjectKind kind, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(database);

        await using var connection = await OpenAsync(connectionString, database, cancellationToken);
        await using var command = new SqlCommand(SqlServerQueries.Objects, connection);
        command.Parameters.Add(new SqlParameter("@type", System.Data.SqlDbType.Char, 2) { Value = TypeCode(kind) });
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var result = new List<SqlSchemaObject>();
        while (await reader.ReadAsync(cancellationToken))
            result.Add(new SqlSchemaObject(reader.GetString(0), reader.GetString(1), kind, []) { Comment = ReadComment(reader, 2) });

        return result;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SqlColumn>> GetColumnsAsync(string connectionString, string database, string schema, string objectName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(database);

        await using var connection = await OpenAsync(connectionString, database, cancellationToken);
        await using var command = new SqlCommand(SqlServerQueries.Columns, connection);
        command.Parameters.Add(new SqlParameter("@schema", System.Data.SqlDbType.NVarChar, 128) { Value = schema });
        command.Parameters.Add(new SqlParameter("@name", System.Data.SqlDbType.NVarChar, 128) { Value = objectName });
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var result = new List<SqlColumn>();
        while (await reader.ReadAsync(cancellationToken))
        {
            var precision = reader.GetByte(3);
            var scale = reader.GetByte(4);
            result.Add(new SqlColumn(reader.GetString(0), FormatType(reader.GetString(1), reader.GetInt16(2), precision, scale))
            {
                IsNullable = reader.GetBoolean(5),
                PrimaryKeyOrdinal = reader.GetInt32(8),
                Precision = precision,
                Scale = scale,
                IsForeignKey = reader.GetBoolean(9),
                IsIdentity = reader.GetBoolean(6),
                IsComputed = reader.GetBoolean(7),
                Comment = ReadComment(reader, 10),
            });
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<SqlDatabaseSchema> GetSchemaAsync(string connectionString, string? database, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenAsync(connectionString, database, cancellationToken);
        await using var command = new SqlCommand(SqlServerQueries.Snapshot, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        await reader.ReadAsync(cancellationToken);
        var databaseName = reader.GetString(0);

        await reader.NextResultAsync(cancellationToken);
        var objects = new List<(int Id, string Schema, string Name, SqlSchemaObjectKind Kind)>();
        while (await reader.ReadAsync(cancellationToken))
        {
            var kind = reader.GetString(3) switch
            {
                "U" => SqlSchemaObjectKind.Table,
                "V" => SqlSchemaObjectKind.View,
                _ => SqlSchemaObjectKind.Procedure,
            };
            objects.Add((reader.GetInt32(0), reader.GetString(1), reader.GetString(2), kind));
        }

        await reader.NextResultAsync(cancellationToken);
        var columns = new Dictionary<int, List<SqlColumn>>();
        while (await reader.ReadAsync(cancellationToken))
        {
            var objectId = reader.GetInt32(0);
            if (!columns.TryGetValue(objectId, out var list))
                columns[objectId] = list = [];

            var precision = reader.GetByte(4);
            var scale = reader.GetByte(5);
            list.Add(new SqlColumn(reader.GetString(1), FormatType(reader.GetString(2), reader.GetInt16(3), precision, scale))
            {
                SqlType = reader.GetString(6),
                IsNullable = reader.GetBoolean(7),
                PrimaryKeyOrdinal = reader.GetInt32(8),
                Precision = precision,
                Scale = scale,
            });
        }

        return new SqlDatabaseSchema(
            databaseName,
            objects.Select(o => new SqlSchemaObject(o.Schema, o.Name, o.Kind, columns.TryGetValue(o.Id, out var list) ? list : [])).ToArray());
    }

    private static string? ReadComment(SqlDataReader reader, int ordinal)
        => reader.IsDBNull(ordinal) || string.IsNullOrWhiteSpace(reader.GetString(ordinal)) ? null : reader.GetString(ordinal).Trim();

    /// <summary>Type affiche comme dans SSMS : longueur, precision ou echelle selon le type de base.</summary>
    private static string FormatType(string typeName, short maxLength, byte precision, byte scale)
    {
        string Length(int divisor) => maxLength == -1 ? "max" : (maxLength / divisor).ToString(CultureInfo.InvariantCulture);

        return typeName.ToLowerInvariant() switch
        {
            "nvarchar" or "nchar" => $"{typeName}({Length(2)})",
            "varchar" or "char" or "varbinary" or "binary" => $"{typeName}({Length(1)})",
            "decimal" or "numeric" => $"{typeName}({precision},{scale})",
            "datetime2" or "time" or "datetimeoffset" => $"{typeName}({scale})",
            "float" when precision != 53 => $"{typeName}({precision})",
            _ => typeName,
        };
    }

    private static string TypeCode(SqlSchemaObjectKind kind) => kind switch
    {
        SqlSchemaObjectKind.Table => "U",
        SqlSchemaObjectKind.View => "V",
        SqlSchemaObjectKind.Procedure => "P",
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
    };

    private static async Task<SqlConnection> OpenAsync(string connectionString, string? database, CancellationToken cancellationToken)
    {
        var builder = new SqlConnectionStringBuilder(connectionString)
        {
            ConnectTimeout = SqlServerConstants.ConnectTimeoutSeconds,
        };
        if (database is not null)
            builder.InitialCatalog = database;

        var connection = new SqlConnection(builder.ConnectionString);
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
