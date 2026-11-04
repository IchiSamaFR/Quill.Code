using Microsoft.Data.Sqlite;
using System.Globalization;
using Quill.Data.Sql.Sqlite.Constants;

namespace Quill.Data.Sql.Sqlite;

/// <summary>
/// Catalogue SQLite lu dans sqlite_master et les pragmas. Les bases sont celles de la connexion (main et bases attachees),
/// chacune son propre schema : <see cref="SqlSchemaObject.Schema"/> porte le nom de la base. Pas de procedures.
/// </summary>
public sealed class SqliteSchemaReader : ISqlSchemaReader
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<SqlDatabaseInfo>> GetDatabasesAsync(string connectionString, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenAsync(connectionString, cancellationToken);
        await using var command = new SqliteCommand(SqliteQueries.Databases, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var result = new List<SqlDatabaseInfo>();
        while (await reader.ReadAsync(cancellationToken))
        {
            var name = reader.GetString(0);
            result.Add(new SqlDatabaseInfo(name, string.Equals(name, SqliteConstants.TempDatabase, StringComparison.OrdinalIgnoreCase)));
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SqlSchemaObject>> GetObjectsAsync(string connectionString, string database, SqlSchemaObjectKind kind, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(database);

        if (kind == SqlSchemaObjectKind.Procedure)
            return [];

        await using var connection = await OpenAsync(connectionString, cancellationToken);
        await using var command = new SqliteCommand(WithDatabase(SqliteQueries.Objects, database), connection);
        command.Parameters.AddWithValue("@type", kind == SqlSchemaObjectKind.View ? SqliteConstants.ViewType : SqliteConstants.TableType);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var result = new List<SqlSchemaObject>();
        while (await reader.ReadAsync(cancellationToken))
            result.Add(new SqlSchemaObject(database, reader.GetString(0), kind, []));

        return result;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SqlColumn>> GetColumnsAsync(string connectionString, string database, string schema, string objectName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(database);

        await using var connection = await OpenAsync(connectionString, cancellationToken);
        await using var command = new SqliteCommand(SqliteQueries.Columns, connection);
        command.Parameters.AddWithValue("@name", objectName);
        command.Parameters.AddWithValue("@schema", string.IsNullOrWhiteSpace(schema) ? database : schema);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var result = new List<SqlColumn>();
        while (await reader.ReadAsync(cancellationToken))
        {
            var hidden = reader.GetInt32Value(4);
            if (hidden == SqliteConstants.HiddenColumn)
                continue;

            result.Add(CreateColumn(reader.GetString(0), reader.GetString(1), reader.GetFlag(2), reader.GetInt32Value(3), hidden) with
            {
                IsForeignKey = reader.GetFlag(5),
            });
        }

        return MarkRowIdAlias(result);
    }

    /// <inheritdoc />
    public async Task<SqlDatabaseSchema> GetSchemaAsync(string connectionString, string? database, CancellationToken cancellationToken = default)
    {
        var databaseName = string.IsNullOrWhiteSpace(database) ? SqliteConstants.MainDatabase : database;

        await using var connection = await OpenAsync(connectionString, cancellationToken);
        await using var command = new SqliteCommand(WithDatabase(SqliteQueries.Snapshot, databaseName), connection);
        command.Parameters.AddWithValue("@schema", databaseName);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var objects = new List<(string Name, SqlSchemaObjectKind Kind, List<SqlColumn> Columns)>();
        while (await reader.ReadAsync(cancellationToken))
        {
            var name = reader.GetString(0);
            if (objects.Count == 0 || objects[^1].Name != name)
                objects.Add((name, reader.GetString(1) == SqliteConstants.ViewType ? SqlSchemaObjectKind.View : SqlSchemaObjectKind.Table, []));

            // Ligne sans colonne : objet dont pragma_table_xinfo ne rend rien.
            if (reader.IsDBNull(2) || reader.GetInt32Value(6) == SqliteConstants.HiddenColumn)
                continue;

            objects[^1].Columns.Add(CreateColumn(reader.GetString(2), reader.GetString(3), reader.GetFlag(4), reader.GetInt32Value(5), reader.GetInt32Value(6)));
        }

        return new SqlDatabaseSchema(
            databaseName,
            objects.Select(o => new SqlSchemaObject(databaseName, o.Name, o.Kind, MarkRowIdAlias(o.Columns))).ToArray());
    }

    private static SqlColumn CreateColumn(string name, string declaredType, bool isNullable, int primaryKeyOrdinal, int hidden)
    {
        var dataType = string.IsNullOrWhiteSpace(declaredType) ? SqliteConstants.UntypedColumn : declaredType.Trim();
        var (precision, scale) = ReadPrecision(dataType);
        var parenthesis = dataType.IndexOf('(', StringComparison.Ordinal);

        return new SqlColumn(name, dataType)
        {
            SqlType = (parenthesis > 0 ? dataType[..parenthesis] : dataType).Trim().ToUpperInvariant(),
            IsNullable = isNullable,
            PrimaryKeyOrdinal = primaryKeyOrdinal,
            Precision = precision,
            Scale = scale,
            IsComputed = hidden >= SqliteConstants.FirstGeneratedColumn,
        };
    }

    /// <summary>Seule colonne d'une cle primaire, de type INTEGER : alias de rowid, numerote par SQLite comme une identity.</summary>
    private static List<SqlColumn> MarkRowIdAlias(List<SqlColumn> columns)
    {
        var keys = columns.Where(c => c.IsPrimaryKey).Take(2).ToList();
        if (keys.Count != 1 || !string.Equals(keys[0].SqlType, SqliteConstants.RowIdAliasType, StringComparison.Ordinal))
            return columns;

        var index = columns.IndexOf(keys[0]);
        columns[index] = keys[0] with { IsIdentity = true, IsNullable = false };
        return columns;
    }

    /// <summary>Precision et echelle d'un type declare comme DECIMAL(10,2) ; 0 si absentes.</summary>
    private static (byte Precision, byte Scale) ReadPrecision(string dataType)
    {
        var open = dataType.IndexOf('(', StringComparison.Ordinal);
        var close = dataType.IndexOf(')', StringComparison.Ordinal);
        if (open < 0 || close < open)
            return (0, 0);

        var parts = dataType[(open + 1)..close].Split(',', StringSplitOptions.TrimEntries);
        return (ParseByte(parts[0]), parts.Length > 1 ? ParseByte(parts[1]) : (byte)0);
    }

    private static byte ParseByte(string text)
        => byte.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) ? value : (byte)0;

    /// <summary>Insere le nom de base, entre guillemets doubles echappes, dans une requete du catalogue.</summary>
    private static string WithDatabase(string query, string database)
        => string.Format(CultureInfo.InvariantCulture, query, $"\"{database.Replace("\"", "\"\"", StringComparison.Ordinal)}\"");

    private static async Task<SqliteConnection> OpenAsync(string connectionString, CancellationToken cancellationToken)
    {
        var connection = new SqliteConnection(connectionString);
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
