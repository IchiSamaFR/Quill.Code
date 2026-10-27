namespace Quill.Code.Analysis;

/// <summary>Erreur, avertissement ou information signalé sur une portion du texte.</summary>
/// <param name="Start">Début de la portion (index dans le texte).</param>
/// <param name="Length">Longueur de la portion ; 0 pour un emplacement (ex. ";" manquant).</param>
/// <param name="Severity">Gravité.</param>
/// <param name="Message">Message affiché au survol et dans la liste des erreurs.</param>
/// <param name="Code">Identifiant du compilateur ou du parseur (CS0103, 102…), null si aucun.</param>
public sealed record CodeDiagnostic(int Start, int Length, CodeDiagnosticSeverity Severity, string Message, string? Code = null)
{
    /// <summary>Fin de la portion.</summary>
    public int End => Start + Length;

    /// <summary>Vrai pour une erreur.</summary>
    public bool IsError => Severity == CodeDiagnosticSeverity.Error;
}
