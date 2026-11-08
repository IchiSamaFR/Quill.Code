using static Quill.Code.Editing.TextLines;

namespace Quill.Code.Editing;

/// <summary>
/// Visual Studio-style editing commands on raw text: each reads the text and the selection, then returns the
/// replacements and the new selection (<see cref="EditResult"/>) that the editor applies as a single undo step.
/// </summary>
/// <remarks>A null result means: nothing special, the key keeps its normal effect.</remarks>
public static class CodeEditing
{
    #region Saisie

    /// <summary>
    /// Enter: keeps the line's indentation, adds a level after "{", and between "{" and "}" opens the block
    /// over three lines. Whitespace after the caret does not become stray indentation.
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

    /// <summary>Ctrl+Enter: empty line above; Ctrl+Shift+Enter: below. Same indentation, current line untouched.</summary>
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
    /// Typed character, before insertion: ( [ { " ' pairs closed automatically, existing closing character skipped,
    /// selection wrapped, "}" unindented on an empty line, "{" on its own line (option). Nothing in
    /// strings or comments.
    /// </summary>
    /// <returns>Null: the character is inserted normally.</returns>
    public static EditResult? TypeCharacter(string text, TextSelection selection, char typed, CodeEditingOptions options)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(options);

        var start = selection.Start;
        var end = selection.End;
        var closing = ClosingOf(typed);
        var region = SourceScanner.RegionAt(text, start, options.Syntax);

        // Selection wrapped by the pair: it stays on the wrapped text.
        if (!selection.IsEmpty)
        {
            return region == SourceRegion.Code && closing is char wrap
                ? EditResult.Replace(start, end - start, typed + text[start..end] + wrap, new TextSelection(start + 1, end + 1))
                : null;
        }

        var next = start < text.Length ? text[start] : '\0';
        var previous = start > 0 ? text[start - 1] : '\0';
        var isQuote = typed is '"' or '\'';

        // Closing character already there (inserted by the pair): simply step over it.
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

        // No pair stuck to a word (it's, l'apostrophe) nor before text.
        if (isQuote && IsIdentifierPart(previous))
            return null;

        if (next != '\0' && !char.IsWhiteSpace(next) && next is not (')' or ']' or '}' or ';' or ','))
            return null;

        return EditResult.Replace(start, 0, $"{typed}{close}", TextSelection.Caret(start + 1));
    }

    /// <summary>Backspace: an empty pair at once; in leading whitespace, a whole indentation level.</summary>
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

    /// <summary>Tab: indents the lines of a multi-line selection, otherwise inserts indentation up to the next tab stop.</summary>
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
    /// Tab / Shift+Tab on lines: adds or removes one indentation level on each non-empty line.
    /// A multi-line selection then covers whole lines.
    /// </summary>
    /// <returns>Null if nothing changes.</returns>
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
    /// Ctrl+K, Ctrl+C / Ctrl+K, Ctrl+U: comment prefix placed at the minimum indentation of non-empty lines, or removed.
    /// </summary>
    /// <returns>Null if the language has no line comment or if nothing changes.</returns>
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

    /// <summary>Ctrl+D: duplicates the selection right after it (the copy is selected), or the current line below.</summary>
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

    /// <summary>Ctrl+Shift+K: deletes the selected lines (or the current line).</summary>
    public static EditResult DeleteLines(string text, TextSelection selection)
    {
        ArgumentNullException.ThrowIfNull(text);

        var (first, last) = SelectedLines(text, selection);

        if (last < text.Length)
            return EditResult.Replace(first, NextLineStart(text, last) - first, string.Empty, TextSelection.Caret(first));

        // Last line: the line break before it is what goes, so as not to leave an empty line.
        if (first > 0)
        {
            var previousEnd = PreviousLineEnd(text, first);
            return EditResult.Replace(previousEnd, last - previousEnd, string.Empty, TextSelection.Caret(LineStart(text, previousEnd)));
        }

        return EditResult.Replace(0, text.Length, string.Empty, TextSelection.Caret(0));
    }

    /// <summary>Alt+Up / Alt+Down: swaps the selected lines with the neighboring line.</summary>
    /// <returns>Null at the top or bottom of the text.</returns>
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

    /// <summary>Home: first non-whitespace character of the line, then start of line if already there.</summary>
    public static int SmartHomePosition(string text, int caret)
    {
        ArgumentNullException.ThrowIfNull(text);

        var lineStart = LineStart(text, caret);
        var firstChar = lineStart + LeadingWhitespaceLength(text, lineStart, LineEnd(text, caret));
        return caret == firstChar ? lineStart : firstChar;
    }

    #endregion

    /// <summary>Closing character of the pair opened by <paramref name="c"/>, null if it is not an opening character.</summary>
    public static char? ClosingOf(char c) => c switch
    {
        '(' => ')',
        '[' => ']',
        '{' => '}',
        '"' => '"',
        '\'' => '\'',
        _ => null,
    };

    /// <summary>"}" typed on an empty line: the line loses one indentation level.</summary>
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

    /// <summary>Allman style: "{" at the end of a code line moves to the next line along with its "}".</summary>
    private static EditResult? PlaceBraceOnNewLine(string text, int start, int lineStart, CodeEditingOptions options)
    {
        var lineEnd = LineEnd(text, start);
        if (text.AsSpan(lineStart, start - lineStart).IsWhiteSpace() || !text.AsSpan(start, lineEnd - start).IsWhiteSpace())
            return null;

        // Trailing whitespace on a code line goes away with the line break.
        var codeEnd = lineStart + text.AsSpan(lineStart, start - lineStart).TrimEnd().Length;
        var indent = text.Substring(lineStart, LeadingWhitespaceLength(text, lineStart, start));
        var inserted = options.NewLine + indent + "{}";

        return EditResult.Replace(codeEnd, lineEnd - codeEnd, inserted, TextSelection.Caret(codeEnd + inserted.Length - 1));
    }

    private static bool IsIdentifierPart(char c) => char.IsLetterOrDigit(c) || c == '_';

    /// <summary>Position after replacements expressed in the original coordinates (ordered, non-overlapping).</summary>
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
