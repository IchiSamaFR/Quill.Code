namespace Quill.Data.Sql;

/// <summary>Table, view or procedure of a database, with its columns (none for a procedure, or if they are not loaded).</summary>
/// <param name="Schema">Owning schema (e.g. <c>dbo</c>).</param>
/// <param name="Name">Name of the object.</param>
/// <param name="Kind">Kind of the object.</param>
/// <param name="Columns">Columns, in table order.</param>
public sealed record SqlSchemaObject(string Schema, string Name, SqlSchemaObjectKind Kind, IReadOnlyList<SqlColumn> Columns)
{
    /// <summary>Comment (MS_Description on SQL Server); null if missing.</summary>
    public string? Comment { get; init; }

    /// <summary>Schema-qualified name (e.g. <c>dbo.Users</c>).</summary>
    public string QualifiedName => $"{Schema}.{Name}";
}
