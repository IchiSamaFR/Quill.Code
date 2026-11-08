using System.Text.RegularExpressions;

namespace Quill.Code.Highlighting;

/// <summary><c>Rule</c> or <c>Keywords</c> rule (one expression) or <c>Span</c> (begin, end).</summary>
internal sealed class SyntaxRule(Regex begin, Regex? end, bool isSpan, bool isMultiline, SyntaxColor? color)
{
    /// <summary>Expression of the rule, or start of the span.</summary>
    public Regex Begin { get; } = begin;

    /// <summary>End of the span; null for a span running to the end of the line.</summary>
    public Regex? End { get; } = end;

    public bool IsSpan { get; } = isSpan;

    /// <summary>The span can continue on the following lines.</summary>
    public bool IsMultiline { get; } = isMultiline;

    public SyntaxColor? Color { get; } = color;
}
