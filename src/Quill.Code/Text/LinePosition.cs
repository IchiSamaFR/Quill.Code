namespace Quill.Code.Text;

/// <summary>Ligne et colonne d'une position dans un texte, comptées à partir de 1.</summary>
/// <param name="Line">Ligne, à partir de 1.</param>
/// <param name="Column">Colonne, à partir de 1.</param>
public readonly record struct LinePosition(int Line, int Column)
{
    /// <summary>
    /// Calcule la ligne et la colonne de <paramref name="offset"/> dans <paramref name="text"/>.
    /// \r\n, \n et \r comptent chacun pour un saut de ligne.
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
