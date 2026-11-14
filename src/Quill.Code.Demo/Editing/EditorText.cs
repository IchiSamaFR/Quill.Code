using System.Windows.Controls;
using System.Windows.Documents;

namespace Quill.Code.Demo.Editing;

/// <summary>
/// Bridge between the RichTextBox and the raw text Quill expects: one line = one paragraph of flat Runs,
/// lines joined by \n. Quill positions (index in the text) thus convert without ambiguity.
/// </summary>
internal static class EditorText
{
    public static IEnumerable<Paragraph> Lines(RichTextBox editor) => editor.Document.Blocks.OfType<Paragraph>();

    public static string GetLineText(Paragraph line) => new TextRange(line.ContentStart, line.ContentEnd).Text;

    public static string GetText(RichTextBox editor) => string.Join('\n', Lines(editor).Select(GetLineText));

    public static void SetText(RichTextBox editor, string text)
    {
        var document = editor.Document;
        document.Blocks.Clear();
        foreach (var line in text.ReplaceLineEndings("\n").Split('\n'))
            document.Blocks.Add(new Paragraph(new Run(line)));

        editor.CaretPosition = document.ContentStart;
    }

    /// <summary>Index in the text of a document position.</summary>
    public static int GetOffset(RichTextBox editor, TextPointer position)
    {
        var target = position.Paragraph;
        var offset = 0;
        foreach (var line in Lines(editor))
        {
            if (ReferenceEquals(line, target))
                return offset + new TextRange(line.ContentStart, position).Text.Length;

            offset += GetLineText(line).Length + 1;
        }

        return Math.Max(0, offset - 1);
    }

    public static int GetCaretOffset(RichTextBox editor) => GetOffset(editor, editor.CaretPosition);

    /// <summary>Document position at a text index.</summary>
    public static TextPointer GetPosition(RichTextBox editor, int offset)
    {
        foreach (var line in Lines(editor))
        {
            var length = GetLineText(line).Length;
            if (offset <= length)
                return GetPosition(line, offset);

            offset -= length + 1;
        }

        return editor.Document.ContentEnd;
    }

    /// <summary>Replaces a text range and puts the caret after the insertion.</summary>
    public static void Replace(RichTextBox editor, int start, int end, string text)
    {
        var range = new TextRange(GetPosition(editor, start), GetPosition(editor, end)) { Text = text };
        editor.CaretPosition = range.End;
    }

    private static TextPointer GetPosition(Paragraph line, int column)
    {
        foreach (var run in line.Inlines.OfType<Run>())
        {
            if (column <= run.Text.Length)
                return run.ContentStart.GetPositionAtOffset(column) ?? run.ContentEnd;

            column -= run.Text.Length;
        }

        return line.ContentEnd;
    }
}
