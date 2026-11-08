namespace Quill.Code.Sql.SqlServer.Completion;

/// <summary>Table (or source) referenced by a query.</summary>
/// <param name="Database">Database, if the name is qualified with three parts.</param>
/// <param name="Schema">Schema, if the name is qualified.</param>
/// <param name="Name">Name of the table.</param>
/// <param name="Alias">Declared alias, otherwise null.</param>
/// <param name="Offset">Position of the reference in the text.</param>
public sealed record SqlTableReference(string? Database, string? Schema, string Name, string? Alias, int Offset)
{
    /// <summary>Name under which the query designates the table: the alias if there is one.</summary>
    public string ExposedName => Alias ?? Name;
}
