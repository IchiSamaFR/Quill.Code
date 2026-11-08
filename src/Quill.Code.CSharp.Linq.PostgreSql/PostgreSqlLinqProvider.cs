using Microsoft.EntityFrameworkCore;
using Npgsql;
using Quill.Data.Sql;
using System.Reflection;

namespace Quill.Code.CSharp.Linq.PostgreSql;

/// <summary>
/// LINQ queries on PostgreSQL: Npgsql.EntityFrameworkCore.PostgreSQL, the provider's default mapping.
/// Keep one instance per application (cache of compiled DbContexts).
/// </summary>
public sealed class PostgreSqlLinqProvider : ILinqDatabaseProvider
{
    private readonly string? _applicationName;

    /// <summary>PostgreSQL provider; <paramref name="applicationName"/> is seen by the server (pg_stat_activity).</summary>
    public PostgreSqlLinqProvider(string? applicationName = null)
    {
        _applicationName = applicationName;
    }

    /// <inheritdoc/>
    public IReadOnlyList<Assembly> Assemblies { get; } = [typeof(NpgsqlDbContextOptionsBuilderExtensions).Assembly, typeof(NpgsqlConnection).Assembly];

    /// <inheritdoc/>
    public string? GetClrType(SqlColumn column)
    {
        ArgumentNullException.ThrowIfNull(column);

        // SqlType is the internal name (pg_type.typname): int4, varchar, timestamptz...
        var sqlType = (string.IsNullOrEmpty(column.SqlType) ? column.DataType : column.SqlType).ToLowerInvariant();

        // Arrays (_int4), ranges, network, geometry: no mapping kept.
        return sqlType switch
        {
            "int2" or "smallint" => "short",
            "int4" or "integer" or "int" => "int",
            "int8" or "bigint" => "long",
            "bool" or "boolean" => "bool",
            "numeric" or "money" => "decimal",
            "float4" or "real" => "float",
            "float8" or "double precision" => "double",
            "date" => "DateOnly",
            "time" => "TimeOnly",
            "timetz" => "DateTimeOffset",
            "timestamp" or "timestamptz" => "DateTime",
            "interval" => "TimeSpan",
            "uuid" => "Guid",
            "text" or "varchar" or "bpchar" or "name" or "citext" or "json" or "jsonb" or "xml" => "string",
            "bytea" => "byte[]",
            _ => null,
        };
    }

    /// <inheritdoc/>
    public string BuildConnectionString(string connectionString, string? database)
    {
        var builder = new NpgsqlConnectionStringBuilder(connectionString);
        if (!string.IsNullOrWhiteSpace(_applicationName))
            builder.ApplicationName = _applicationName;
        if (!string.IsNullOrWhiteSpace(database))
            builder.Database = database;

        return builder.ConnectionString;
    }

    /// <inheritdoc/>
    public void Configure(DbContextOptionsBuilder builder, string connectionString)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.UseNpgsql(connectionString);
    }
}
