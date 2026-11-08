namespace Quill.Code.Analysis;

/// <summary>Severity of a diagnostic.</summary>
public enum CodeDiagnosticSeverity
{
    /// <summary>Information or suggestion.</summary>
    Info,

    /// <summary>Warning (yellow underline).</summary>
    Warning,

    /// <summary>Error (red underline).</summary>
    Error,
}
