namespace Quill.Code.Text;

/// <summary>Line and column of a position in a text, counted from 1.</summary>
/// <param name="Line">Line, from 1.</param>
/// <param name="Column">Column, from 1.</param>
public readonly record struct LinePosition(int Line, int Column)
{
    /// <summary>
    /// Computes the line and column of <paramref name="offset"/> in <paramref name="text"/>.
    /// \r\n, \n and \r each count as one line break.
    /// </summary>
    public static LinePosition FromOffset(string text, int offset)
    {
        ArgumentNullException.ThrowIfNull(text);

        var end = Math.Clamp(offset, 0, text.Length);
        var line = 1;
        var lineStart = 0;
        for (var i = 0; i < end; i++)
        {
            var c = text[i];
            if (c == '\n' || (c == '\r' && (i + 1 >= text.Length || text[i + 1] != '\n')))
            {
                line++;
                lineStart = i + 1;
            }
        }

        return new LinePosition(line, end - lineStart + 1);
    }
}
