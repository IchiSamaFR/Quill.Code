namespace Quill.Code.Sql.Completion;

/// <summary>Table (ou source) citée par une requête.</summary>
/// <param name="Database">Base, si le nom est qualifié sur trois parties.</param>
/// <param name="Schema">Schéma, si le nom est qualifié.</param>
/// <param name="Name">Nom de la table.</param>
/// <param name="Alias">Alias déclaré, sinon null.</param>
/// <param name="Offset">Position de la référence dans le texte.</param>
public sealed record SqlTableReference(string? Database, string? Schema, string Name, string? Alias, int Offset)
{
    /// <summary>Nom sous lequel la requête désigne la table : l'alias s'il existe.</summary>
    public string ExposedName => Alias ?? Name;
}
