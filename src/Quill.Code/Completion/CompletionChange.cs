namespace Quill.Code.Completion;

/// <summary>Change to apply to the text when a suggestion is committed.</summary>
/// <param name="SpanStart">Start of the replaced span.</param>
/// <param name="SpanLength">Length of the replaced span.</param>
/// <param name="NewText">Text inserted in its place.</param>
/// <param name="NewCaretPosition">Caret position in the new text; end of the insertion if null.</param>
public sealed record CompletionChange(int SpanStart, int SpanLength, string NewText, int? NewCaretPosition = null)
{
    /// <summary>Caret position after the change is applied.</summary>
    public int CaretPosition => NewCaretPosition ?? SpanStart + NewText.Length;
}
