using static Quill.Code.Editing.TextLines;

namespace Quill.Code.Editing;

/// <summary>
/// Commandes d'édition à la Visual Studio, sur du texte brut : chacune lit le texte et la sélection, puis rend les
/// remplacements et la nouvelle sélection (<see cref="EditResult"/>) que l'éditeur applique en une étape d'annulation.
/// </summary>
/// <remarks>Un résultat null signifie : rien de particulier, la touche garde son effet normal.</remarks>
public static class CodeEditing
{
    #region Saisie

    /// <summary>
    /// Entrée : reprend l'indentation de la ligne, ajoute un niveau après « { », et entre « { » et « } » ouvre le bloc
    /// sur trois lignes. Les blancs qui suivent le curseur ne deviennent pas une indentation parasite.
    /// </summary>
    public static EditResult InsertNewLine(string text, TextSelection selection, CodeEditingOptions options)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(options);

        var start = selection.Start;
        var lineStart = LineStart(text, start);
        var indentLength = LeadingWhitespaceLength(text, lineStart, start);
        var indent = text.Substring(lineStart, indentLength);
        var isInIndent = indentLength == start - lineStart;

        var after = selection.End;
        while (!isInIndent && after < text.Length && text[after] is ' ' or '\t')
            after++;

        var before = start;
        while (before > lineStart && text[before - 1] is ' ' or '\t')
            before--;

        var opensBlock = before > lineStart && text[before - 1] == '{';
        var inserted = options.NewLine + indent + (opensBlock ? options.IndentUnit : string.Empty);
        var caret = start + inserted.Length;

        if (opensBlock && after < text.Length && text[after] == '}')
            inserted += options.NewLine + indent;

        return EditResult.Replace(start, after - start, inserted, TextSelection.Caret(caret));
    }

    /// <summary>Ctrl+Entrée : ligne vide au-dessus ; Ctrl+Maj+Entrée : en dessous. Même indentation, ligne courante intacte.</summary>
    public static EditResult OpenLine(string text, TextSelection selection, bool above, CodeEditingOptions options)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(options);

        var lineStart = LineStart(text, selection.Active);
        var lineEnd = LineEnd(text, selection.Active);
        var indent = text.Substring(lineStart, LeadingWhitespaceLength(text, lineStart, lineEnd));

        return above
            ? EditResult.Replace(lineStart, 0, indent + options.NewLine, TextSelection.Caret(lineStart + indent.Length))
            : EditResult.Replace(lineEnd, 0, options.NewLine + indent, TextSelection.Caret(lineEnd + options.NewLine.Length + indent.Length));
    }

    /// <summary>
    /// Caractère tapé, avant son insertion : paires ( [ { " ' fermées automatiquement, fermant déjà présent sauté,
    /// sélection entourée, « } » désindentée sur une ligne vide, « { » sur sa propre ligne (option). Rien dans les
    /// chaînes ni les commentaires.
    /// </summary>
    /// <returns>Null : le caractère s'insère normalement.</returns>
    public static EditResult? TypeCharacter(string text, TextSelection selection, char typed, CodeEditingOptions options)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(options);

        var start = selection.Start;
        var end = selection.End;
        var closing = ClosingOf(typed);
        var region = SourceScanner.RegionAt(text, start, options.Syntax);

        // Sélection entourée par la paire : elle reste sur le texte entouré.
        if (!selection.IsEmpty)
        {
            return region == SourceRegion.Code && closing is char wrap
                ? EditResult.Replace(start, end - start, typed + text[start..end] + wrap, new TextSelection(start + 1, end + 1))
                : null;
        }

        var next = start < text.Length ? text[start] : '\0';
        var previous = start > 0 ? text[start - 1] : '\0';
        var isQuote = typed is '"' or '\'';

        // Fermant déjà là (inséré par la paire) : on passe simplement par-dessus.
        if (next == typed && (isQuote ? region == SourceRegion.String : region == SourceRegion.Code && typed is ')' or ']' or '}'))
            return EditResult.Move(TextSelection.Caret(start + 1));

        if (region != SourceRegion.Code)
            return null;

        var lineStart = LineStart(text, start);

        if (typed == '}')
            return OutdentClosingBrace(text, start, lineStart, options);

        if (closing is not char close)
            return null;

        if (typed == '{' && options.PlaceOpenBraceOnNewLine && PlaceBraceOnNewLine(text, start, lineStart, options) is { } allman)
            return allman;

        // Pas de paire collée à un mot (it's, l'apostrophe) ni avant du texte.
        if (isQuote && IsIdentifierPart(previous))
            return null;

        if (next != '\0' && !char.IsWhiteSpace(next) && next is not (')' or ']' or '}' or ';' or ','))
            return null;

        return EditResult.Replace(start, 0, $"{typed}{close}", TextSelection.Caret(start + 1));
    }

    /// <summary>Retour arrière : une paire vide d'un coup ; dans les espaces de tête, un niveau d'indentation entier.</summary>
    public static EditResult? DeleteBackward(string text, TextSelection selection, CodeEditingOptions options)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(options);

        var position = selection.Active;
        if (!selection.IsEmpty || position <= 0 || position > text.Length)
            return null;

        if (position < text.Length && ClosingOf(text[position - 1]) is char closing && text[position] == closing)
            return EditResult.Replace(position - 1, 2, string.Empty, TextSelection.Caret(position - 1));

        if (options.UseTabs || text[position - 1] != ' ')
            return null;

        var lineStart = LineStart(text, position);
        var column = position - lineStart;
        if (LeadingWhitespaceLength(text, lineStart, position) != column || text.AsSpan(lineStart, column).Contains('\t'))
            return null;

        var size = options.IndentSize;
        var count = Math.Min(column % size == 0 ? size : column % size, column);
        return EditResult.Replace(position - count, count, string.Empty, TextSelection.Caret(position - count));
    }

    #endregion

    #region Indentation

    /// <summary>Tab : indente les lignes d'une sélection multiligne, sinon insère une indentation jusqu'au taquet suivant.</summary>
    public static EditResult InsertTab(string text, TextSelection selection, CodeEditingOptions options)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(options);

        if (text.AsSpan(selection.Start, selection.Length).Contains('\n'))
            return IndentLines(text, selection, outdent: false, options) ?? EditResult.Move(selection);

        var inserted = "\t";
        if (!options.UseTabs)
        {
            var size = options.IndentSize;
            var column = VisualColumn(text, LineStart(text, selection.Start), selection.Start, size);
            inserted = new string(' ', size - column % size);
        }

        return EditResult.Replace(selection.Start, selection.Length, inserted, TextSelection.Caret(selection.Start + inserted.Length));
    }

    /// <summary>
    /// Tab / Maj+Tab sur des lignes : ajoute ou retire un niveau d'indentation à chaque ligne non vide.
    /// Une sélection multiligne couvre ensuite les lignes entières.
    /// </summary>
    /// <returns>Null si rien ne change.</returns>
    public static EditResult? IndentLines(string text, TextSelection selection, bool outdent, CodeEditingOptions options)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(options);

        var (first, last) = SelectedLines(text, selection);
        List<TextEdit> edits = [];
        foreach (var (lineStart, lineEnd) in Lines(text, first, last))
        {
            if (!outdent)
            {
                if (lineEnd > lineStart)
                    edits.Add(new TextEdit(lineStart, 0, options.IndentUnit));
                continue;
            }

            var count = text[lineStart..lineEnd].StartsWith('\t') ? 1 : 0;
            if (count == 0)
            {
                while (count < options.IndentSize && lineStart + count < lineEnd && text[lineStart + count] == ' ')
                    count++;
            }

            if (count > 0)
                edits.Add(new TextEdit(lineStart, count, string.Empty));
        }

        if (edits.Count == 0)
            return null;

        var result = new EditResult(edits, selection);
        if (selection.IsEmpty)
            return result with { Selection = TextSelection.Caret(Math.Max(LineStart(text, selection.Active), MapOffset(selection.Active, edits))) };

        var newLast = MapOffset(last, edits);
        return result with { Selection = new TextSelection(first, newLast) };
    }

    #endregion

    #region Lignes

    /// <summary>
    /// Ctrl+K, Ctrl+C / Ctrl+K, Ctrl+U : préfixe de commentaire posé à l'indentation minimale des lignes non vides, ou retiré.
    /// </summary>
    /// <returns>Null si le langage n'a pas de commentaire de ligne ou si rien ne change.</returns>
    public static EditResult? ToggleLineComment(string text, TextSelection selection, bool comment, CodeEditingOptions options)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(options);

        var prefix = options.Syntax.LineComment;
        if (string.IsNullOrEmpty(prefix))
            return null;

        var (first, last) = SelectedLines(text, selection);
        var lines = Lines(text, first, last);

        List<TextEdit> edits = [];
        if (comment)
        {
            var filled = lines.Where(l => LeadingWhitespaceLength(text, l.Start, l.End) < l.End - l.Start).ToList();
            if (filled.Count == 0)
                return null;

            var inserted = options.InsertSpaceAfterLineComment ? prefix + " " : prefix;
            var indent = filled.Min(l => LeadingWhitespaceLength(text, l.Start, l.End));
            edits.AddRange(filled.Select(l => new TextEdit(l.Start + indent, 0, inserted)));
        }
        else
        {
            foreach (var (lineStart, lineEnd) in lines)
            {
                var at = lineStart + LeadingWhitespaceLength(text, lineStart, lineEnd);
                if (!text.AsSpan(at, lineEnd - at).StartsWith(prefix, StringComparison.Ordinal))
                    continue;

                var length = prefix.Length;
                if (options.InsertSpaceAfterLineComment && at + length < lineEnd && text[at + length] == ' ')
                    length++;

                edits.Add(new TextEdit(at, length, string.Empty));
            }
        }

        if (edits.Count == 0)
            return null;

        return new EditResult(edits, new TextSelection(MapOffset(selection.Anchor, edits), MapOffset(selection.Active, edits)));
    }

    /// <summary>Ctrl+D : duplique la sélection juste après elle (la copie est sélectionnée), ou la ligne courante en dessous.</summary>
    public static EditResult DuplicateSelectionOrLine(string text, TextSelection selection, CodeEditingOptions options)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(options);

        if (!selection.IsEmpty)
        {
            var copy = text.Substring(selection.Start, selection.Length);
            return EditResult.Replace(selection.End, 0, copy, new TextSelection(selection.End, selection.End + copy.Length));
        }

        var lineStart = LineStart(text, selection.Active);
        var lineEnd = LineEnd(text, selection.Active);
        var line = text[lineStart..lineEnd];
        return EditResult.Replace(lineEnd, 0, options.NewLine + line, TextSelection.Caret(selection.Active + options.NewLine.Length + line.Length));
    }

    /// <summary>Ctrl+Maj+K : supprime les lignes de la sélection (ou la ligne courante).</summary>
    public static EditResult DeleteLines(string text, TextSelection selection)
    {
        ArgumentNullException.ThrowIfNull(text);

        var (first, last) = SelectedLines(text, selection);

        if (last < text.Length)
            return EditResult.Replace(first, NextLineStart(text, last) - first, string.Empty, TextSelection.Caret(first));

        // Dernière ligne : c'est le saut de ligne qui la précède qui part, pour ne pas laisser de ligne vide.
        if (first > 0)
        {
            var previousEnd = PreviousLineEnd(text, first);
            return EditResult.Replace(previousEnd, last - previousEnd, string.Empty, TextSelection.Caret(LineStart(text, previousEnd)));
        }

        return EditResult.Replace(0, text.Length, string.Empty, TextSelection.Caret(0));
    }

    /// <summary>Alt+Haut / Alt+Bas : échange les lignes de la sélection avec la ligne voisine.</summary>
    /// <returns>Null en haut ou en bas du texte.</returns>
    public static EditResult? MoveLines(string text, TextSelection selection, bool up)
    {
        ArgumentNullException.ThrowIfNull(text);

        var (first, last) = SelectedLines(text, selection);
        var block = text[first..last];

        if (up)
        {
            if (first == 0)
                return null;

            var previousEnd = PreviousLineEnd(text, first);
            var previousStart = LineStart(text, previousEnd);
            var previous = text[previousStart..previousEnd];
            var separator = text[previousEnd..first];
            var shift = -(previous.Length + separator.Length);

            return EditResult.Replace(previousStart, last - previousStart, block + separator + previous,
                new TextSelection(selection.Anchor + shift, selection.Active + shift));
        }

        if (last >= text.Length)
            return null;

        var nextStart = NextLineStart(text, last);
        var nextEnd = LineEnd(text, nextStart);
        var nextLine = text[nextStart..nextEnd];
        var nextSeparator = text[last..nextStart];
        var delta = nextLine.Length + nextSeparator.Length;

        return EditResult.Replace(first, nextEnd - first, nextLine + nextSeparator + block,
            new TextSelection(selection.Anchor + delta, selection.Active + delta));
    }

    /// <summary>Début : premier caractère non blanc de la ligne, puis début de ligne s'il y est déjà.</summary>
    public static int SmartHomePosition(string text, int caret)
    {
        ArgumentNullException.ThrowIfNull(text);

        var lineStart = LineStart(text, caret);
        var firstChar = lineStart + LeadingWhitespaceLength(text, lineStart, LineEnd(text, caret));
        return caret == firstChar ? lineStart : firstChar;
    }

    #endregion

    /// <summary>Caractère fermant de la paire ouverte par <paramref name="c"/>, null si ce n'est pas un ouvrant.</summary>
    public static char? ClosingOf(char c) => c switch
    {
        '(' => ')',
        '[' => ']',
        '{' => '}',
        '"' => '"',
        '\'' => '\'',
        _ => null,
    };

    /// <summary>« } » tapée sur une ligne vide : la ligne perd un niveau d'indentation.</summary>
    private static EditResult? OutdentClosingBrace(string text, int start, int lineStart, CodeEditingOptions options)
    {
        var indent = start - lineStart;
        if (indent == 0 || LeadingWhitespaceLength(text, lineStart, start) != indent)
            return null;

        var count = text[start - 1] == '\t' ? 1 : 0;
        if (count == 0)
        {
            while (count < options.IndentSize && count < indent && text[start - 1 - count] == ' ')
                count++;
        }

        return EditResult.Replace(start - count, count, "}", TextSelection.Caret(start - count + 1));
    }

    /// <summary>Style Allman : « { » en fin de ligne de code passe sur la ligne suivante avec sa « } ».</summary>
    private static EditResult? PlaceBraceOnNewLine(string text, int start, int lineStart, CodeEditingOptions options)
    {
        var lineEnd = LineEnd(text, start);
        if (text.AsSpan(lineStart, start - lineStart).IsWhiteSpace() || !text.AsSpan(start, lineEnd - start).IsWhiteSpace())
            return null;

        // Les blancs en fin de ligne de code partent avec le passage à la ligne.
        var codeEnd = lineStart + text.AsSpan(lineStart, start - lineStart).TrimEnd().Length;
        var indent = text.Substring(lineStart, LeadingWhitespaceLength(text, lineStart, start));
        var inserted = options.NewLine + indent + "{}";

        return EditResult.Replace(codeEnd, lineEnd - codeEnd, inserted, TextSelection.Caret(codeEnd + inserted.Length - 1));
    }

    private static bool IsIdentifierPart(char c) => char.IsLetterOrDigit(c) || c == '_';

    /// <summary>Position après des remplacements exprimés dans les coordonnées d'origine (ordonnés, sans chevauchement).</summary>
    private static int MapOffset(int offset, IEnumerable<TextEdit> edits)
    {
        var shift = 0;
        foreach (var edit in edits)
        {
            if (edit.End <= offset)
                shift += edit.NewText.Length - edit.Length;
            else if (edit.Start < offset)
                shift += edit.Start + edit.NewText.Length - offset;
        }

        return offset + shift;
    }
}
