using System.Text;

namespace Quill.Code.Editing;

/// <summary>
/// Result of an editing command: replacements expressed in the original text (ordered, non-overlapping),
/// to apply as a single undo step, and the selection in the modified text.
/// </summary>
public sealed record EditResult(IReadOnlyList<TextEdit> Edits, TextSelection Selection)
{
    /// <summary>Applies the replacements to <paramref name="text"/>.</summary>
    public string Apply(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        StringBuilder builder = new(text.Length);
        var position = 0;
        foreach (var edit in Edits)
        {
            builder.Append(text, position, edit.Start - position).Append(edit.NewText);
            position = edit.End;
        }

        return builder.Append(text, position, text.Length - position).ToString();
    }

    /// <summary>
    /// Single replacement covering all of the result's replacements (for a control that applies one span at a time),
    /// null if there is nothing to replace (caret move only).
    /// </summary>
    public TextEdit? ToSingleEdit(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        switch (Edits.Count)
        {
            case 0:
                return null;
            case 1:
                return Edits[0];
        }

        var start = Edits[0].Start;
        var end = Edits[^1].End;
        var applied = Apply(text);
        var length = applied.Length - (text.Length - (end - start));
        return new TextEdit(start, end - start, applied.Substring(start, length));
    }

    internal static EditResult Replace(int start, int length, string newText, TextSelection selection)
        => new([new TextEdit(start, length, newText)], selection);

    internal static EditResult Move(TextSelection selection) => new([], selection);
}
