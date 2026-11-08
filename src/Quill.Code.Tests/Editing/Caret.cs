using Quill.Code.Editing;

namespace Quill.Code.Tests.Editing;

/// <summary>Test notation: "|" marks the caret, "[" and "]" the start and end of a selection.</summary>
internal static class Caret
{
    public static (string Text, TextSelection Selection) Parse(string marked)
    {
        var caret = marked.IndexOf('|', StringComparison.Ordinal);
        if (caret >= 0)
            return (marked.Remove(caret, 1), TextSelection.Caret(caret));

        var start = marked.IndexOf('[', StringComparison.Ordinal);
        var end = marked.IndexOf(']', StringComparison.Ordinal) - 1;
        return (marked.Remove(end + 1, 1).Remove(start, 1), new TextSelection(start, end));
    }

    public static string Render(string text, EditResult? result)
    {
        Assert.NotNull(result);

        var applied = result.Apply(text);
        var selection = result.Selection;
        return selection.IsEmpty
            ? applied.Insert(selection.Active, "|")
            : applied.Insert(selection.End, "]").Insert(selection.Start, "[");
    }
}
