namespace Quill.Code.Sql.Schema;

/// <summary>Table, vue ou procédure d'une base, avec ses colonnes (aucune pour une procédure).</summary>
/// <param name="Schema">Schéma propriétaire (ex. <c>dbo</c>).</param>
/// <param name="Name">Nom de l'objet.</param>
/// <param name="Kind">Nature de l'objet.</param>
/// <param name="Columns">Colonnes, dans l'ordre de la table.</param>
public sealed record SqlSchemaObject(string Schema, string Name, SqlSchemaObjectKind Kind, IReadOnlyList<SqlColumn> Columns)
{
    /// <summary>Nom qualifié par le schéma (ex. <c>dbo.Users</c>).</summary>
    public string QualifiedName => $"{Schema}.{Name}";
}
