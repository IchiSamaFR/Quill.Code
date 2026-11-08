using Quill.Data.Sql.Constants;

namespace Quill.Data.Sql;

/// <summary>Complete schema of a database: objects and columns, indexed for repeated lookups (completion, hover).</summary>
public sealed class SqlDatabaseSchema
{
    private readonly HashSet<string> _schemas;

    // Objects by name, dbo first: Find is called again for each table of a query, over thousands of objects.
    private readonly Dictionary<string, SqlSchemaObject[]> _byName;

    /// <param name="name">Name of the database.</param>
    /// <param name="objects">Tables, views and procedures of the database.</param>
    public SqlDatabaseSchema(string name, IReadOnlyList<SqlSchemaObject> objects)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(objects);

        Name = name;
        Objects = objects;
        Schemas = objects.Select(o => o.Schema).Distinct(StringComparer.OrdinalIgnoreCase).Order(StringComparer.OrdinalIgnoreCase).ToArray();
        _schemas = new HashSet<string>(Schemas, StringComparer.OrdinalIgnoreCase);
        _byName = objects
            .GroupBy(o => o.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                g => g.Key,
                g => g.OrderBy(o => IsDefaultSchema(o.Schema) ? 0 : 1).ToArray(),
                StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>Name of the database.</summary>
    public string Name { get; }

    /// <summary>Tables, views and procedures.</summary>
    public IReadOnlyList<SqlSchemaObject> Objects { get; }

    /// <summary>Schemas owning at least one object, sorted.</summary>
    public IReadOnlyList<string> Schemas { get; }

    /// <summary>True for the default schema (<see cref="SqlSchemaConstants.DefaultSchema"/>).</summary>
    public static bool IsDefaultSchema(string schema) => string.Equals(schema, SqlSchemaConstants.DefaultSchema, StringComparison.OrdinalIgnoreCase);

    /// <summary>Object by name (case-insensitive); without a schema, <see cref="SqlSchemaConstants.DefaultSchema"/> is preferred.</summary>
    public SqlSchemaObject? Find(string? schema, string name)
    {
        ArgumentNullException.ThrowIfNull(name);

        if (!_byName.TryGetValue(name, out var candidates))
            return null;

        if (schema is null)
            return candidates[0];

        return Array.Find(candidates, c => string.Equals(c.Schema, schema, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>True if an object of the database belongs to <paramref name="schema"/>.</summary>
    public bool HasSchema(string schema) => _schemas.Contains(schema);
}
