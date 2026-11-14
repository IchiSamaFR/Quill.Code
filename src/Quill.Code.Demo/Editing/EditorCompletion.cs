using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using Quill.Code.Completion;

namespace Quill.Code.Demo.Editing;

/// <summary>
/// Completion list of the RichTextBox: opened while typing a name, after a dot and on Ctrl+Space. The
/// suggestions come from the Quill service, once per word; further typing only filters them
/// (<see cref="CompletionFilter"/>).
/// </summary>
internal sealed class EditorCompletion
{
    private readonly RichTextBox _editor;
    private readonly Popup _popup;
    private readonly ListBox _list;
    private readonly Func<ICodeLanguageService?> _service;

    private Session? _session;
    private string? _typed;
    private int _version;

    public EditorCompletion(RichTextBox editor, Popup popup, ListBox list, Func<ICodeLanguageService?> service)
    {
        _editor = editor;
        _popup = popup;
        _list = list;
        _service = service;

        _editor.PreviewTextInput += (_, e) => _typed = e.Text;
        _editor.PreviewKeyDown += OnPreviewKeyDown;
        _editor.SelectionChanged += (_, _) => OnSelectionChanged();
        _list.PreviewMouseLeftButtonUp += (_, e) =>
        {
            // A click on the scroll bar picks nothing.
            if (e.OriginalSource is DependencyObject source && ItemsControl.ContainerFromElement(_list, source) is ListBoxItem)
                _ = CommitAsync();
        };
    }

    /// <summary>True while the application rewrites the document (highlighting): the caret moves without the user leaving their word.</summary>
    public bool IsSuspended { get; set; }

    /// <summary>To call after each text change made by the user.</summary>
    public void OnTextChanged()
    {
        var typed = _typed;
        _typed = null;

        if (typed is { Length: 1 } && (IsIdentifierChar(typed[0]) || typed[0] == '.'))
        {
            // After a dot, the suggestions are the member's: never those of the previous word.
            if (typed[0] == '.')
                _session = null;

            _ = RefreshAsync(isExplicit: false);
        }
        else if (typed is null && _session is not null)
        {
            // Backspace: the list follows the shortened word.
            _ = RefreshAsync(isExplicit: false);
        }
        else
        {
            Close();
        }
    }

    public void Close()
    {
        _version++;
        _session = null;
        _popup.IsOpen = false;
    }

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Space && Keyboard.Modifiers == ModifierKeys.Control)
        {
            e.Handled = true;
            _session = null;
            _ = RefreshAsync(isExplicit: true);
            return;
        }

        if (!_popup.IsOpen)
            return;

        switch (e.Key)
        {
            case Key.Down:
                Move(1);
                break;
            case Key.Up:
                Move(-1);
                break;
            case Key.Enter or Key.Tab:
                if (_list.SelectedItem is not CompletionItem
                    || (_session?.IsSuggestionMode == true && _list.SelectedIndex < 0))
                    return;

                e.Handled = true;
                _ = CommitAsync();
                return;
            case Key.Escape:
                Close();
                break;
            default:
                return;
        }

        e.Handled = true;
    }

    /// <summary>The caret left the starting word (arrows, click): the list no longer makes sense.</summary>
    private void OnSelectionChanged()
    {
        if (IsSuspended || _session is not { } session)
            return;

        var text = EditorText.GetText(_editor);
        if (FindWordStart(text, EditorText.GetCaretOffset(_editor)) != session.WordStart)
            Close();
    }

    private async Task RefreshAsync(bool isExplicit)
    {
        try
        {
            var text = EditorText.GetText(_editor);
            var caret = EditorText.GetCaretOffset(_editor);
            var wordStart = FindWordStart(text, caret);

            if (_session is null || _session.WordStart != wordStart)
            {
                // No list while typing a number (TOP 10, = 3).
                if (_service() is not { } service || (!isExplicit && wordStart < caret && char.IsDigit(text[wordStart])))
                {
                    Close();
                    return;
                }

                // Off the UI thread: Roslyn and the SQL analysis have a synchronous part.
                var version = ++_version;
                var list = await Task.Run(() => service.GetCompletionsAsync(text, caret));
                if (version != _version)
                    return;

                _session = new Session(wordStart, list.SpanStart, list.Items, list.IsSuggestionMode);

                // The text may have moved during the request.
                text = EditorText.GetText(_editor);
                caret = EditorText.GetCaretOffset(_editor);
                if (FindWordStart(text, caret) != wordStart)
                {
                    Close();
                    return;
                }
            }

            Show(CompletionFilter.Filter(_session.Candidates, text[wordStart..caret]), _session.IsSuggestionMode);
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException or OperationCanceledException)
        {
            Close();
        }
    }

    private void Show(IReadOnlyList<CompletionItem> items, bool isSuggestionMode)
    {
        if (items.Count == 0)
        {
            Close();
            return;
        }

        _list.ItemsSource = items;
        _list.SelectedIndex = isSuggestionMode ? -1 : 0;

        var caret = _editor.CaretPosition.GetCharacterRect(LogicalDirection.Forward);
        _popup.HorizontalOffset = caret.Left;
        _popup.VerticalOffset = caret.Bottom;
        _popup.IsOpen = true;
    }

    private void Move(int delta)
    {
        if (_list.Items.Count == 0)
            return;

        _list.SelectedIndex = Math.Clamp(_list.SelectedIndex + delta, 0, _list.Items.Count - 1);
        _list.ScrollIntoView(_list.SelectedItem);
    }

    /// <summary>Replaces the current word with the chosen suggestion.</summary>
    private async Task CommitAsync()
    {
        var item = _list.SelectedItem as CompletionItem;
        var session = _session;
        var caret = EditorText.GetCaretOffset(_editor);
        Close();

        if (item is null || session is null || caret < session.SpanStart || _service() is not { } service)
            return;

        try
        {
            var text = EditorText.GetText(_editor);
            if (item.Data is not null)
            {
                var fresh = await service.GetCompletionsAsync(text, caret);
                if (EditorText.GetText(_editor) != text)
                    return;

                item = fresh.Items.FirstOrDefault(candidate =>
                    candidate.Label == item.Label
                    && candidate.Kind == item.Kind
                    && candidate.TextToInsert == item.TextToInsert
                    && candidate.FilterText == item.FilterText);
                if (item is null)
                    return;

                var change = await service.GetChangeAsync(text, item);
                if (EditorText.GetText(_editor) != text)
                    return;

                EditorText.Replace(_editor, change.SpanStart, change.SpanStart + change.SpanLength, change.NewText);
                _editor.CaretPosition = EditorText.GetPosition(_editor, Math.Clamp(change.CaretPosition, 0, EditorText.GetText(_editor).Length));
            }
            else
            {
                EditorText.Replace(_editor, session.SpanStart, caret, item.TextToInsert);
            }

            _editor.Focus();
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException or OperationCanceledException)
        {
        }
    }

    private static int FindWordStart(string text, int caret)
    {
        var start = Math.Clamp(caret, 0, text.Length);
        while (start > 0 && IsIdentifierChar(text[start - 1]))
            start--;

        return start;
    }

    private static bool IsIdentifierChar(char c) => char.IsLetterOrDigit(c) || c is '_' or '@' or '#' or '$';

    /// <summary>Suggestions requested for the word starting at <paramref name="WordStart"/>.</summary>
    private sealed record Session(int WordStart, int SpanStart, IReadOnlyList<CompletionItem> Candidates, bool IsSuggestionMode);
}
