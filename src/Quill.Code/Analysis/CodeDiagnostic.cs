namespace Quill.Code.Analysis;

/// <summary>Error, warning or information reported on a span of the text.</summary>
/// <param name="Start">Start of the span (index in the text).</param>
/// <param name="Length">Length of the span; 0 for a location (e.g. missing ";").</param>
/// <param name="Severity">Severity.</param>
/// <param name="Message">Message shown on hover and in the error list.</param>
/// <param name="Code">Compiler or parser identifier (CS0103, 102…), null if none.</param>
public sealed record CodeDiagnostic(int Start, int Length, CodeDiagnosticSeverity Severity, string Message, string? Code = null)
{
    /// <summary>End of the span.</summary>
    public int End => Start + Length;

    /// <summary>True for an error.</summary>
    public bool IsError => Severity == CodeDiagnosticSeverity.Error;
}
