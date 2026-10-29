namespace Quill.Data.Sql;

/// <summary>Table, vue ou procédure d'une base, avec ses colonnes (aucune pour une procédure, ou si elles ne sont pas chargées).</summary>
/// <param name="Schema">Schéma propriétaire (ex. <c>dbo</c>).</param>
/// <param name="Name">Nom de l'objet.</param>
/// <param name="Kind">Nature de l'objet.</param>
/// <param name="Columns">Colonnes, dans l'ordre de la table.</param>
public sealed record SqlSchemaObject(string Schema, string Name, SqlSchemaObjectKind Kind, IReadOnlyList<SqlColumn> Columns)
{
    /// <summary>Commentaire (MS_Description sous SQL Server) ; null si absent.</summary>
    public string? Comment { get; init; }

    /// <summary>Nom qualifié par le schéma (ex. <c>dbo.Users</c>).</summary>
    public string QualifiedName => $"{Schema}.{Name}";
}
