using Microsoft.EntityFrameworkCore;
using Oracle.EntityFrameworkCore.Infrastructure;
using Oracle.ManagedDataAccess.Client;
using Quill.Data.Sql;
using System.Reflection;

namespace Quill.Code.CSharp.Linq.Oracle;

/// <summary>
/// LINQ queries on Oracle: Oracle.EntityFrameworkCore on ODP.NET Core, generated SQL compatible with Oracle 19c.
/// An Oracle connection holds a single database: the requested database is ignored. Keep one instance per application.
/// </summary>
public sealed class OracleLinqProvider : ILinqDatabaseProvider
{
    /// <inheritdoc/>
    public IReadOnlyList<Assembly> Assemblies { get; } = [typeof(OracleDbContextOptionsBuilder).Assembly, typeof(OracleConnection).Assembly];

    /// <inheritdoc/>
    public string? GetClrType(SqlColumn column)
    {
        ArgumentNullException.ThrowIfNull(column);

        // SqlType is DATA_TYPE: NUMBER, VARCHAR2, TIMESTAMP(6) WITH TIME ZONE...
        var sqlType = (string.IsNullOrEmpty(column.SqlType) ? column.DataType : column.SqlType).ToUpperInvariant();

        if (sqlType.StartsWith("TIMESTAMP", StringComparison.Ordinal))
            return sqlType.EndsWith("WITH TIME ZONE", StringComparison.Ordinal) && !sqlType.Contains("LOCAL", StringComparison.Ordinal) ? "DateTimeOffset" : "DateTime";
        if (sqlType.StartsWith("INTERVAL DAY", StringComparison.Ordinal))
            return "TimeSpan";

        var parenthesis = sqlType.IndexOf('(', StringComparison.Ordinal);
        if (parenthesis > 0)
            sqlType = sqlType[..parenthesis];

        // ROWID, XMLTYPE, SDO_GEOMETRY, INTERVAL YEAR...: no mapping without a dedicated package.
        return sqlType switch
        {
            "NUMBER" => NumberType(column.Precision, column.Scale),
            "FLOAT" or "BINARY_DOUBLE" => "double",
            "BINARY_FLOAT" => "float",
            "VARCHAR2" or "NVARCHAR2" or "CHAR" or "NCHAR" or "CLOB" or "NCLOB" or "LONG" => "string",
            "DATE" => "DateTime",
            "RAW" or "BLOB" or "LONG RAW" => "byte[]",
            _ => null,
        };
    }

    /// <inheritdoc/>
    public string BuildConnectionString(string connectionString, string? database)
        => new OracleConnectionStringBuilder(connectionString).ConnectionString;

    /// <inheritdoc/>
    public void Configure(DbContextOptionsBuilder builder, string connectionString)
    {
        ArgumentNullException.ThrowIfNull(builder);

        // Lowest supported dialect: avoids 23ai SQL (BOOLEAN, JSON...) on an older server.
        builder.UseOracle(connectionString, oracle => oracle.UseOracleSQLCompatibility(OracleSQLCompatibility.DatabaseVersion19));
    }

    /// <summary>NUMBER(p, 0): integer according to its width, like EF Core Oracle's default mapping; otherwise decimal.</summary>
    private static string NumberType(byte precision, byte scale) => (precision, scale) switch
    {
        (1, 0) => "bool",
        ( > 0 and <= 4, 0) => "short",
        ( > 0 and <= 9, 0) => "int",
        ( > 0 and <= 18, 0) => "long",
        _ => "decimal",
    };
}
