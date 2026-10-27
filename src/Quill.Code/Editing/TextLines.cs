namespace Quill.Code.Editing;

/// <summary>Repérage des lignes d'un texte ; « \n » et « \r\n » sont acceptés.</summary>
public static class TextLines
{
    /// <summary>Début de la ligne qui contient <paramref name="position"/>.</summary>
    public static int LineStart(string text, int position)
    {
        ArgumentNullException.ThrowIfNull(text);

        position = Math.Clamp(position, 0, text.Length);
        return position == 0 ? 0 : text.LastIndexOf('\n', position - 1) + 1;
    }

    /// <summary>Fin de la ligne qui contient <paramref name="position"/>, avant son saut de ligne.</summary>
    public static int LineEnd(string text, int position)
    {
        ArgumentNullException.ThrowIfNull(text);

        position = Math.Clamp(position, 0, text.Length);
        var newLine = text.IndexOf('\n', position);
        if (newLine < 0)
            return text.Length;

        return newLine > position && text[newLine - 1] == '\r' ? newLine - 1 : newLine;
    }

    /// <summary>Début de la ligne suivante (après le saut de ligne qui commence à <paramref name="lineEnd"/>).</summary>
    public static int NextLineStart(string text, int lineEnd)
    {
        ArgumentNullException.ThrowIfNull(text);

        if (lineEnd >= text.Length)
            return text.Length;

        return text[lineEnd] == '\r' && lineEnd + 1 < text.Length && text[lineEnd + 1] == '\n' ? lineEnd + 2 : lineEnd + 1;
    }

    /// <summary>Fin de la ligne précédente (début de son saut de ligne) ; 0 pour la première ligne.</summary>
    public static int PreviousLineEnd(string text, int lineStart)
    {
        ArgumentNullException.ThrowIfNull(text);

        if (lineStart <= 0)
            return 0;

        return lineStart >= 2 && text[lineStart - 2] == '\r' ? lineStart - 2 : lineStart - 1;
    }

    /// <summary>Nombre d'espaces et tabulations à partir de <paramref name="start"/>, sans dépasser <paramref name="end"/>.</summary>
    public static int LeadingWhitespaceLength(string text, int start, int end)
    {
        ArgumentNullException.ThrowIfNull(text);

        var index = start;
        while (index < end && text[index] is ' ' or '\t')
            index++;

        return index - start;
    }

    /// <summary>
    /// Lignes touchées par la sélection : début de la première, fin de la dernière. Une sélection qui finit
    /// en début de ligne n'inclut pas cette ligne.
    /// </summary>
    public static (int Start, int End) SelectedLines(string text, TextSelection selection)
    {
        ArgumentNullException.ThrowIfNull(text);

        var end = selection.End;
        if (!selection.IsEmpty && end == LineStart(text, end))
            end = Math.Max(selection.Start, PreviousLineEnd(text, end));

        return (LineStart(text, selection.Start), LineEnd(text, end));
    }

    /// <summary>Colonne affichée : une tabulation va au taquet suivant.</summary>
    public static int VisualColumn(string text, int lineStart, int position, int tabSize)
    {
        ArgumentNullException.ThrowIfNull(text);

        var column = 0;
        for (var i = lineStart; i < position; i++)
            column = text[i] == '\t' ? (column / tabSize + 1) * tabSize : column + 1;

        return column;
    }

    /// <summary>Lignes (début, fin) de <paramref name="start"/> à <paramref name="end"/> compris.</summary>
    internal static List<(int Start, int End)> Lines(string text, int start, int end)
    {
        List<(int Start, int End)> lines = [];
        for (var lineStart = start; ; lineStart = NextLineStart(text, LineEnd(text, lineStart)))
        {
            var lineEnd = LineEnd(text, lineStart);
            lines.Add((lineStart, lineEnd));
            if (lineEnd >= end || lineEnd >= text.Length)
                return lines;
        }
    }
}
