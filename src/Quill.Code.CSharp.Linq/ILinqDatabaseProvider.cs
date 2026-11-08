using Microsoft.EntityFrameworkCore;
using Quill.Data.Sql;
using System.Reflection;

namespace Quill.Code.CSharp.Linq;

/// <summary>
/// Database engine of a LINQ query (e.g. <c>SqlServerLinqProvider</c> from <c>Quill.Code.CSharp.Linq.SqlServer</c>):
/// column types of the generated DbContext, EF provider references and connection. The same instance for
/// the editor and the execution: compiled DbContexts and references are cached per provider.
/// </summary>
public interface ILinqDatabaseProvider
{
    /// <summary>Assemblies of the EF provider and its driver, referenced by queries (e.g. engine-specific <c>EF.Functions</c>).</summary>
    IReadOnlyList<Assembly> Assemblies { get; }

    /// <summary>C# type of a column, without nullability (e.g. <c>int</c>, <c>string</c>); null: not mappable, the column is ignored.</summary>
    string? GetClrType(SqlColumn column);

    /// <summary>
    /// Connection string of an execution, on <paramref name="database"/> (null: the one in the string);
    /// throws <see cref="ArgumentException"/> or <see cref="FormatException"/> if it is invalid.
    /// </summary>
    string BuildConnectionString(string connectionString, string? database);

    /// <summary>Connects the EF context to the database (e.g. <c>UseSqlServer</c>).</summary>
    void Configure(DbContextOptionsBuilder builder, string connectionString);
}
