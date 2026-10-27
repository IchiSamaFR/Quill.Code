namespace Quill.Code.Highlighting;

/// <summary>Portion de texte colorée (positions en unités UTF-16).</summary>
/// <param name="Start">Début de la portion.</param>
/// <param name="Length">Longueur de la portion.</param>
/// <param name="Color">Mise en forme ; null pour une règle sans couleur (texte reconnu mais laissé à la couleur par défaut).</param>
public readonly record struct SyntaxToken(int Start, int Length, SyntaxColor? Color)
{
    /// <summary>Fin de la portion.</summary>
    public int End => Start + Length;
}
