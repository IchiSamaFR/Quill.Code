namespace Quill.Code.Analysis;

/// <summary>Infobulle d'un nom survolé.</summary>
/// <param name="Start">Début du nom : l'infobulle reste ouverte tant que la souris est dessus.</param>
/// <param name="Length">Longueur du nom.</param>
/// <param name="Signature">Ligne principale (ex. "(propriété) string Student.Name").</param>
/// <param name="Description">Précision facultative (documentation, table d'origine…).</param>
public sealed record HoverInfo(int Start, int Length, string Signature, string? Description = null)
{
    /// <summary>Fin du nom.</summary>
    public int End => Start + Length;
}
