namespace Quill.Code.Analysis;

/// <summary>Gravité d'un diagnostic.</summary>
public enum CodeDiagnosticSeverity
{
    /// <summary>Information ou suggestion.</summary>
    Info,

    /// <summary>Avertissement (souligné en jaune).</summary>
    Warning,

    /// <summary>Erreur (souligné en rouge).</summary>
    Error,
}
