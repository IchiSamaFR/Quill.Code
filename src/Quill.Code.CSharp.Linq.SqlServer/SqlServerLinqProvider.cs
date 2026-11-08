using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Quill.Data.Sql;
using System.Reflection;

namespace Quill.Code.CSharp.Linq.SqlServer;

/// <summary>
/// LINQ queries on SQL Server: default EF Core SQL Server mapping, Microsoft.Data.SqlClient for the connection.
/// Keep one instance per application (cache of compiled DbContexts).
/// </summary>
public sealed class SqlServerLinqProvider : ILinqDatabaseProvider
{
    private readonly string? _applicationName;

    /// <summary>SQL Server provider; <paramref name="applicationName"/> is seen by the server (sys.dm_exec_sessions).</summary>
    public SqlServerLinqProvider(string? applicationName = null)
    {
        _applicationName = applicationName;
    }

    /// <inheritdoc/>
    public IReadOnlyList<Assembly> Assemblies { get; } = [typeof(SqlServerDbContextOptionsExtensions).Assembly, typeof(SqlConnection).Assembly];

    /// <inheritdoc/>
    public string? GetClrType(SqlColumn column)
    {
        ArgumentNullException.ThrowIfNull(column);

        var sqlType = (string.IsNullOrEmpty(column.SqlType) ? column.DataType : column.SqlType).ToLowerInvariant();
        var parenthesis = sqlType.IndexOf('(', StringComparison.Ordinal);
        if (parenthesis > 0)
            sqlType = sqlType[..parenthesis];

        // sql_variant, hierarchyid, geography...: no EF mapping without a dedicated package.
        return sqlType switch
        {
            "bigint" => "long",
            "int" => "int",
            "smallint" => "short",
            "tinyint" => "byte",
            "bit" => "bool",
            "decimal" or "numeric" or "money" or "smallmoney" => "decimal",
            "float" => "double",
            "real" => "float",
            "date" => "DateOnly",
            "time" => "TimeSpan",
            "datetime" or "datetime2" or "smalldatetime" => "DateTime",
            "datetimeoffset" => "DateTimeOffset",
            "uniqueidentifier" => "Guid",
            "char" or "varchar" or "nchar" or "nvarchar" or "text" or "ntext" or "xml" or "sysname" => "string",
            "binary" or "varbinary" or "image" or "timestamp" or "rowversion" => "byte[]",
            _ => null,
        };
    }

    /// <inheritdoc/>
    public string BuildConnectionString(string connectionString, string? database)
    {
        var builder = new SqlConnectionStringBuilder(connectionString);
        if (!string.IsNullOrWhiteSpace(_applicationName))
            builder.ApplicationName = _applicationName;
        if (!string.IsNullOrWhiteSpace(database))
            builder.InitialCatalog = database;

        return builder.ConnectionString;
    }

    /// <inheritdoc/>
    public void Configure(DbContextOptionsBuilder builder, string connectionString)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.UseSqlServer(connectionString);
    }
}
