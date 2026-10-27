namespace Quill.Code.Highlighting;

/// <summary>
/// État transmis d'une ligne à la suivante : le span multiligne encore ouvert (commentaire /* */, chaîne verbatim…).
/// Permet de ne recolorer que les lignes modifiées, puis les suivantes tant que l'état propagé change.
/// </summary>
public readonly record struct SyntaxLineState
{
    internal SyntaxLineState(int openSpan) => OpenSpan = openSpan;

    /// <summary>Début de document : aucun span ouvert.</summary>
    public static SyntaxLineState Initial { get; } = new(-1);

    /// <summary>Index de la règle du span ouvert, -1 si aucun.</summary>
    internal int OpenSpan { get; }

    /// <summary>Vrai si un span multiligne est ouvert.</summary>
    public bool IsInSpan => OpenSpan >= 0;
}
