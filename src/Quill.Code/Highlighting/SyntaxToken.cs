namespace Quill.Code.Highlighting;

/// <summary>Colored span of text (positions in UTF-16 units).</summary>
/// <param name="Start">Start of the span.</param>
/// <param name="Length">Length of the span.</param>
/// <param name="Color">Formatting; null for a rule without color (text recognized but left in the default color).</param>
public readonly record struct SyntaxToken(int Start, int Length, SyntaxColor? Color)
{
    /// <summary>End of the span.</summary>
    public int End => Start + Length;
}
