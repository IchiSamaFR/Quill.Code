using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using Quill.Code.Highlighting;

namespace Quill.Code.Demo.Editing;

/// <summary>
/// Applies to the RichTextBox the colored spans returned by <see cref="SyntaxHighlighter"/>: Quill only produces
/// positions and colors, display stays with the application. Only lines whose split changes are redone.
/// </summary>
internal sealed class EditorHighlighter
{
    private readonly Dictionary<string, Brush> _brushes = new(StringComparer.OrdinalIgnoreCase);
    private readonly Brush _defaultBrush;

    public EditorHighlighter(Brush defaultBrush)
    {
        _defaultBrush = defaultBrush;
    }

    public void Apply(RichTextBox editor, SyntaxDefinition definition)
    {
        var text = EditorText.GetText(editor);
        var tokens = SyntaxHighlighter.Highlight(definition, text);
        var caret = EditorText.GetCaretOffset(editor);
        var isChanged = false;

        editor.BeginChange();
        try
        {
            var lineStart = 0;
            var tokenIndex = 0;
            foreach (var line in EditorText.Lines(editor).ToArray())
            {
                var lineEnd = lineStart + EditorText.GetLineText(line).Length;

                // Quill spans never cross a line end.
                List<(string Text, Brush Brush)> segments = [];
                var position = lineStart;
                while (tokenIndex < tokens.Count && tokens[tokenIndex].Start < lineEnd)
                {
                    var token = tokens[tokenIndex++];
                    if (token.Length == 0 || token.Start < position)
                        continue;

                    var tokenEnd = Math.Min(token.End, lineEnd);
                    if (token.Start > position)
                        segments.Add((text[position..token.Start], _defaultBrush));

                    segments.Add((text[token.Start..tokenEnd], GetBrush(token.Color)));
                    position = tokenEnd;
                }

                if (position < lineEnd)
                    segments.Add((text[position..lineEnd], _defaultBrush));

                if (!Matches(line, segments))
                {
                    line.Inlines.Clear();
                    foreach (var (segmentText, brush) in segments)
                        line.Inlines.Add(new Run(segmentText) { Foreground = brush });

                    isChanged = true;
                }

                lineStart = lineEnd + 1;
            }
        }
        finally
        {
            editor.EndChange();
        }

        // The Runs of the caret line may have been replaced: it gets back its place in the text.
        if (isChanged)
            editor.CaretPosition = EditorText.GetPosition(editor, caret);
    }

    private static bool Matches(Paragraph line, List<(string Text, Brush Brush)> segments)
    {
        if (line.Inlines.Count != segments.Count)
            return false;

        var index = 0;
        foreach (var inline in line.Inlines)
        {
            var (text, brush) = segments[index++];
            if (inline is not Run run || run.Text != text || !ReferenceEquals(run.Foreground, brush))
                return false;
        }

        return true;
    }

    private Brush GetBrush(SyntaxColor? color)
    {
        if (color?.Foreground is not { Length: > 0 } foreground)
            return _defaultBrush;

        if (_brushes.TryGetValue(foreground, out var brush))
            return brush;

        try
        {
            brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(foreground));
            brush.Freeze();
        }
        catch (FormatException)
        {
            brush = _defaultBrush;
        }

        _brushes[foreground] = brush;
        return brush;
    }
}
