namespace Quill.Code.Execution;

/// <summary>Column of a result set.</summary>
/// <param name="Name">Name returned by the server, or a substitute label if the column has none.</param>
/// <param name="TypeName">SQL type of the column (e.g. "nvarchar").</param>
public sealed record QueryResultColumn(string Name, string TypeName);
