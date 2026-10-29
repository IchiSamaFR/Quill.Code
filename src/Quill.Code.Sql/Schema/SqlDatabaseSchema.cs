namespace Quill.Code.Sql.Schema;

/// <summary>Schéma complet d'une base : objets et colonnes, indexés pour les recherches répétées à chaque frappe.</summary>
public sealed class SqlDatabaseSchema
{
    /// <summary>Schéma préféré pour un nom non qualifié, comme le fait SQL Server.</summary>
    public const string DefaultSchema = "dbo";

    private readonly HashSet<string> _schemas;

    // Objets par nom, dbo en tête : Find est rappelé pour chaque table de la requête, sur des milliers d'objets.
    private readonly Dictionary<string, SqlSchemaObject[]> _byName;

    /// <param name="name">Nom de la base.</param>
    /// <param name="objects">Tables, vues et procédures de la base.</param>
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

    /// <summary>Nom de la base.</summary>
    public string Name { get; }

    /// <summary>Tables, vues et procédures.</summary>
    public IReadOnlyList<SqlSchemaObject> Objects { get; }

    /// <summary>Schémas possédant au moins un objet, triés.</summary>
    public IReadOnlyList<string> Schemas { get; }

    /// <summary>Vrai pour le schéma par défaut (<see cref="DefaultSchema"/>).</summary>
    public static bool IsDefaultSchema(string schema) => string.Equals(schema, DefaultSchema, StringComparison.OrdinalIgnoreCase);

    /// <summary>Objet par nom (sans casse) ; sans schéma, <see cref="DefaultSchema"/> est préféré.</summary>
    public SqlSchemaObject? Find(string? schema, string name)
    {
        ArgumentNullException.ThrowIfNull(name);

        if (!_byName.TryGetValue(name, out var candidates))
            return null;

        if (schema is null)
            return candidates[0];

        return Array.Find(candidates, c => string.Equals(c.Schema, schema, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>Vrai si un objet de la base appartient à <paramref name="schema"/>.</summary>
    public bool HasSchema(string schema) => _schemas.Contains(schema);
}
