using Microsoft.EntityFrameworkCore;
using Microting.EntityFrameworkCore.MySql.Infrastructure;
using MySqlConnector;
using Quill.Data.Sql;
using System.Collections.Concurrent;
using System.Reflection;

namespace Quill.Code.CSharp.Linq.MySql;

/// <summary>
/// LINQ queries on MySQL / MariaDB: Microting.EntityFrameworkCore.MySql (EF Core 10 fork of Pomelo) on MySqlConnector.
/// The server version, required by the provider, is detected on first execution then kept per server.
/// Keep one instance per application (cache of compiled DbContexts).
/// </summary>
public sealed class MySqlLinqProvider : ILinqDatabaseProvider
{
    private readonly string? _applicationName;
    private readonly ConcurrentDictionary<string, ServerVersion> _serverVersions = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>MySQL provider; <paramref name="applicationName"/> is seen by the server (program_name connection attribute).</summary>
    public MySqlLinqProvider(string? applicationName = null)
    {
        _applicationName = applicationName;
    }

    /// <inheritdoc/>
    public IReadOnlyList<Assembly> Assemblies { get; } = [typeof(MySqlDbContextOptionsBuilderExtensions).Assembly, typeof(MySqlConnection).Assembly];

    /// <inheritdoc/>
    public string? GetClrType(SqlColumn column)
    {
        ArgumentNullException.ThrowIfNull(column);

        // DataType est le COLUMN_TYPE complet : signe et largeur d'affichage (tinyint(1) = booleen, comme le fournisseur).
        var columnType = column.DataType.ToLowerInvariant();
        var unsigned = columnType.Contains("unsigned", StringComparison.Ordinal);
        var sqlType = (string.IsNullOrEmpty(column.SqlType) ? columnType : column.SqlType).ToLowerInvariant();
        var parenthesis = sqlType.IndexOf('(', StringComparison.Ordinal);
        if (parenthesis > 0)
            sqlType = sqlType[..parenthesis];

        // geometry, point... : pas de mapping sans paquet dedie.
        return sqlType switch
        {
            "tinyint" when columnType.StartsWith("tinyint(1)", StringComparison.Ordinal) => "bool",
            "tinyint" => unsigned ? "byte" : "sbyte",
            "smallint" => unsigned ? "ushort" : "short",
            "mediumint" or "int" or "integer" => unsigned ? "uint" : "int",
            "bigint" => unsigned ? "ulong" : "long",
            "bit" when columnType is "bit" or "bit(1)" => "bool",
            "bit" => "ulong",
            "bool" or "boolean" => "bool",
            "decimal" or "numeric" or "dec" or "fixed" => "decimal",
            "float" => "float",
            "double" or "real" => "double",
            "date" => "DateOnly",
            "time" => "TimeSpan",
            "datetime" or "timestamp" => "DateTime",
            "year" => "short",
            "char" or "varchar" or "tinytext" or "text" or "mediumtext" or "longtext" or "enum" or "set" or "json" => "string",
            "binary" or "varbinary" or "tinyblob" or "blob" or "mediumblob" or "longblob" => "byte[]",
            _ => null,
        };
    }

    /// <inheritdoc/>
    public string BuildConnectionString(string connectionString, string? database)
    {
        var builder = new MySqlConnectionStringBuilder(connectionString) { AllowUserVariables = true };
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

        // Le schema du modele est le nom de la base, deja choisie par la connexion : MySQL n'a pas de schema distinct.
        builder.UseMySql(connectionString, GetServerVersion(connectionString), mySql => mySql.SchemaBehavior(MySqlSchemaBehavior.Ignore));
    }

    private ServerVersion GetServerVersion(string connectionString)
    {
        var target = new MySqlConnectionStringBuilder(connectionString);
        return _serverVersions.GetOrAdd($"{target.Server}:{target.Port}", _ => ServerVersion.AutoDetect(connectionString));
    }
}
