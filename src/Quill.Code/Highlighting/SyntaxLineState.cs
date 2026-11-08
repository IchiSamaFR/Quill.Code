namespace Quill.Code.Highlighting;

/// <summary>
/// State passed from one line to the next: the multi-line span still open (/* */ comment, verbatim string…).
/// Allows recoloring only the modified lines, then the following ones as long as the propagated state changes.
/// </summary>
public readonly record struct SyntaxLineState
{
    internal SyntaxLineState(int openSpan) => OpenSpan = openSpan;

    /// <summary>Start of document: no open span.</summary>
    public static SyntaxLineState Initial { get; } = new(-1);

    /// <summary>Index of the open span's rule, -1 if none.</summary>
    internal int OpenSpan { get; }

    /// <summary>True if a multi-line span is open.</summary>
    public bool IsInSpan => OpenSpan >= 0;
}
