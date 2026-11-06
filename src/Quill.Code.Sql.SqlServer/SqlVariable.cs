namespace Quill.Code.Sql.SqlServer;

/// <summary>Variable fournie par l'hôte, utilisable sans DECLARE (ex. paramètre d'exécution).</summary>
/// <param name="Name">Nom, avec ou sans <c>@</c>.</param>
/// <param name="Type">Type SQL affiché (ex. <c>int</c>).</param>
/// <param name="Description">Origine affichée au survol (ex. « Paramètre du dashboard »).</param>
public sealed record SqlVariable(string Name, string Type, string? Description = null)
{
    /// <summary>Nom tel qu'il s'écrit dans le texte, avec <c>@</c>.</summary>
    public string Reference => "@" + Name.TrimStart('@');
}
