using Quill.Code.Execution;
using Quill.Data.Sql;

namespace Quill.Code.CSharp.Linq;

/// <summary>Context of a <see cref="LinqLanguageService"/>: what gives meaning to the names in the query.</summary>
public sealed record LinqLanguageOptions
{
    /// <summary>Database engine (e.g. <c>SqlServerLinqProvider</c>); null: <see cref="Schema"/> is ignored, C# only.</summary>
    public ILinqDatabaseProvider? Provider { get; init; }

    /// <summary>
    /// Structure of the queried database, read again on each request (an opening connection enriches the answers without
    /// recreating the service); null, a null structure or an exception: C# only, <c>Db</c> being an empty DbContext.
    /// The same instance gives the same compiled DbContext: read it from a cache.
    /// </summary>
    public Func<CancellationToken, Task<SqlDatabaseSchema?>>? Schema { get; init; }

    /// <summary>Parameters provided at execution, usable by name (without @); only names and types matter here.</summary>
    public IReadOnlyList<QueryParameter> Parameters { get; init; } = [];
}
