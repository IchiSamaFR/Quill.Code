using Microsoft.EntityFrameworkCore;
using Oracle.EntityFrameworkCore.Infrastructure;
using Oracle.ManagedDataAccess.Client;
using Quill.Data.Sql;
using System.Reflection;

namespace Quill.Code.CSharp.Linq.Oracle;

/// <summary>
/// Requetes LINQ sur Oracle : Oracle.EntityFrameworkCore sur ODP.NET Core, SQL genere compatible Oracle 19c.
/// Une connexion Oracle porte une seule base : la base demandee est ignoree. A garder en une instance par application.
/// </summary>
public sealed class OracleLinqProvider : ILinqDatabaseProvider
{
    /// <inheritdoc/>
    public IReadOnlyList<Assembly> Assemblies { get; } = [typeof(OracleDbContextOptionsBuilder).Assembly, typeof(OracleConnection).Assembly];

    /// <inheritdoc/>
    public string? GetClrType(SqlColumn column)
    {
        ArgumentNullException.ThrowIfNull(column);

        // SqlType est DATA_TYPE : NUMBER, VARCHAR2, TIMESTAMP(6) WITH TIME ZONE...
        var sqlType = (string.IsNullOrEmpty(column.SqlType) ? column.DataType : column.SqlType).ToUpperInvariant();

        if (sqlType.StartsWith("TIMESTAMP", StringComparison.Ordinal))
            return sqlType.EndsWith("WITH TIME ZONE", StringComparison.Ordinal) && !sqlType.Contains("LOCAL", StringComparison.Ordinal) ? "DateTimeOffset" : "DateTime";
        if (sqlType.StartsWith("INTERVAL DAY", StringComparison.Ordinal))
            return "TimeSpan";

        var parenthesis = sqlType.IndexOf('(', StringComparison.Ordinal);
        if (parenthesis > 0)
            sqlType = sqlType[..parenthesis];

        // ROWID, XMLTYPE, SDO_GEOMETRY, INTERVAL YEAR... : pas de mapping sans paquet dedie.
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

        // Plus petit dialecte pris en charge : evite le SQL 23ai (BOOLEAN, JSON...) sur un serveur plus ancien.
        builder.UseOracle(connectionString, oracle => oracle.UseOracleSQLCompatibility(OracleSQLCompatibility.DatabaseVersion19));
    }

    /// <summary>NUMBER(p, 0) : entier selon sa largeur, comme le mapping par defaut d'EF Core Oracle ; sinon decimal.</summary>
    private static string NumberType(byte precision, byte scale) => (precision, scale) switch
    {
        (1, 0) => "bool",
        ( > 0 and <= 4, 0) => "short",
        ( > 0 and <= 9, 0) => "int",
        ( > 0 and <= 18, 0) => "long",
        _ => "decimal",
    };
}
