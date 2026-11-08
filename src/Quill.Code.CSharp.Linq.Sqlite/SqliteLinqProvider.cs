using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Quill.Data.Sql;
using System.Reflection;

namespace Quill.Code.CSharp.Linq.Sqlite;

/// <summary>
/// LINQ queries on SQLite: Microsoft.EntityFrameworkCore.Sqlite. Declared types follow SQLite's affinity
/// rules (INT, CHAR, REAL...); one database per file, the requested database is ignored. Keep one instance per application.
/// </summary>
public sealed class SqliteLinqProvider : ILinqDatabaseProvider
{
    /// <inheritdoc/>
    public IReadOnlyList<Assembly> Assemblies { get; } = [typeof(SqliteDbContextOptionsBuilderExtensions).Assembly, typeof(SqliteConnection).Assembly];

    /// <inheritdoc/>
    public string? GetClrType(SqlColumn column)
    {
        ArgumentNullException.ThrowIfNull(column);

        var sqlType = (string.IsNullOrEmpty(column.SqlType) ? column.DataType : column.SqlType).ToUpperInvariant();

        // Common names first (BOOLEAN, DATETIME, GUID...), then SQLite affinity: a declared type is free-form.
        if (sqlType.Contains("BOOL", StringComparison.Ordinal))
            return "bool";
        if (sqlType.Contains("DATETIME", StringComparison.Ordinal) || sqlType.Contains("TIMESTAMP", StringComparison.Ordinal))
            return "DateTime";
        if (sqlType == "DATE")
            return "DateOnly";
        if (sqlType == "TIME")
            return "TimeOnly";
        if (sqlType is "GUID" or "UUID" or "UNIQUEIDENTIFIER")
            return "Guid";
        if (sqlType.Contains("DECIMAL", StringComparison.Ordinal) || sqlType.Contains("NUMERIC", StringComparison.Ordinal) || sqlType.Contains("MONEY", StringComparison.Ordinal))
            return "decimal";
        if (sqlType.Contains("INT", StringComparison.Ordinal))
            return "long";
        if (sqlType.Contains("CHAR", StringComparison.Ordinal) || sqlType.Contains("CLOB", StringComparison.Ordinal) || sqlType.Contains("TEXT", StringComparison.Ordinal))
            return "string";
        if (sqlType.Contains("BLOB", StringComparison.Ordinal))
            return "byte[]";
        if (sqlType.Contains("REAL", StringComparison.Ordinal) || sqlType.Contains("FLOA", StringComparison.Ordinal) || sqlType.Contains("DOUB", StringComparison.Ordinal))
            return "double";

        // Column without a type (ANY): values of any kind, read as text.
        return "string";
    }

    /// <inheritdoc/>
    public string BuildConnectionString(string connectionString, string? database)
        => new SqliteConnectionStringBuilder(connectionString).ConnectionString;

    /// <inheritdoc/>
    public void Configure(DbContextOptionsBuilder builder, string connectionString)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.UseSqlite(connectionString);
    }
}
