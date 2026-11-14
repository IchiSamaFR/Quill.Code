using System.Data.Common;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Quill.Code.Analysis;
using Quill.Code.Demo.Constants;
using Quill.Code.Demo.Databases;
using Quill.Code.Demo.Editing;
using Quill.Code.Demo.Scenarios;
using Quill.Code.Highlighting;

namespace Quill.Code.Demo;

/// <summary>
/// Demo window: a RichTextBox wired to the Quill services (highlighting, completion, hover,
/// diagnostics) for the chosen scenario. Execution is in MainWindow.Execution.cs.
/// </summary>
public partial class MainWindow : Window
{
    private readonly DemoDatabase _database = new();
    private readonly Dictionary<string, SyntaxDefinition> _syntaxes = new(StringComparer.Ordinal);
    private readonly EditorHighlighter _highlighter;
    private readonly EditorCompletion _completion;
    private readonly DispatcherTimer _highlightTimer;
    private readonly DispatcherTimer _diagnosticTimer;
    private readonly DispatcherTimer _hoverTimer;

    private DemoScenario? _scenario;
    private SyntaxDefinition? _syntax;
    private ICodeLanguageService? _service;
    private Point _hoverPoint;
    private bool _isUpdating;
    private int _diagnosticVersion;

    /// <summary>Creates the window; the demo database is prepared when it loads.</summary>
    public MainWindow()
    {
        InitializeComponent();

        Editor.Document.PageWidth = DemoConstants.EditorPageWidth;
        _highlighter = new EditorHighlighter(Editor.Foreground);
        _completion = new EditorCompletion(Editor, CompletionPopup, CompletionList, () => _service);

        _highlightTimer = CreateTimer(DemoConstants.HighlightDelayMilliseconds, Highlight);
        _diagnosticTimer = CreateTimer(DemoConstants.DiagnosticDelayMilliseconds, () => _ = RefreshDiagnosticsAsync());
        _hoverTimer = CreateTimer(DemoConstants.HoverDelayMilliseconds, () => _ = RefreshHoverAsync());

        // Wired here and not in XAML: nothing must arrive before the fields above exist.
        Editor.TextChanged += (_, _) => OnEditorTextChanged();
        Editor.PreviewKeyDown += OnEditorPreviewKeyDown;
        Editor.MouseMove += OnEditorMouseMove;
        Editor.MouseLeave += (_, _) => _hoverTimer.Stop();
        DataObject.AddPastingHandler(Editor, OnEditorPasting);
        ScenarioBox.SelectionChanged += (_, _) => SelectScenario();
        RunButton.Click += (_, _) => _ = RunAsync();
        StopButton.Click += (_, _) => _runCancellation?.Cancel();
        Deactivated += (_, _) => _completion.Close();
        Closing += (_, _) => _runCancellation?.Cancel();
        Loaded += (_, _) => _ = InitializeAsync();
    }

    private static DispatcherTimer CreateTimer(int milliseconds, Action tick)
    {
        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(milliseconds) };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            tick();
        };
        return timer;
    }

    // ---------- Startup and scenario choice ----------

    private async Task InitializeAsync()
    {
        try
        {
            var errors = await _database.CreateAsync();
            MessagesBox.Text = errors.Count == 0
                ? $"Demo database ready: {_database.ConnectionString}"
                : string.Join(Environment.NewLine, errors.Select(e => e.Text));
        }
        catch (Exception ex) when (ex is DbException or IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            // Without a database: the fictitious catalogs remain usable.
            MessagesBox.Text = $"Demo database unavailable: {ex.Message}";
        }

        ScenarioBox.ItemsSource = DemoScenarios.All;
        ScenarioBox.SelectedIndex = 0;
    }

    private void SelectScenario()
    {
        if (ScenarioBox.SelectedItem is not DemoScenario scenario)
            return;

        _scenario = scenario;
        _syntax = GetSyntax(scenario.HighlightingResource);
        _service = DemoLanguages.Create(scenario, _database);
        _completion.Close();

        RunButton.IsEnabled = scenario.CanRun && _database.Schema is not null;
        HintText.Text = scenario.CanRun
            ? "SQLite database with fake data: Ctrl+Space completes, F5 runs the selection or the whole text."
            : "Fictitious catalog, no connection: Ctrl+Space completes, hover describes, nothing runs.";
        HoverText.Text = string.Empty;

        _isUpdating = true;
        try
        {
            EditorText.SetText(Editor, DemoResources.Read(scenario.SampleResource));
        }
        finally
        {
            _isUpdating = false;
        }

        Highlight();
        _diagnosticTimer.Stop();
        _diagnosticTimer.Start();
        Editor.Focus();
    }

    private SyntaxDefinition GetSyntax(string resource)
    {
        if (!_syntaxes.TryGetValue(resource, out var syntax))
            _syntaxes[resource] = syntax = XshdReader.Parse(DemoResources.Read(resource));

        return syntax;
    }

    // ---------- Typing ----------

    private void OnEditorTextChanged()
    {
        if (_isUpdating)
            return;

        Restart(_highlightTimer);
        Restart(_diagnosticTimer);
        _completion.OnTextChanged();
    }

    private void OnEditorPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Handled)
            return;

        if (e.Key == Key.F5)
        {
            e.Handled = true;
            _ = RunAsync();
        }
        else if (e.Key == Key.Enter && Keyboard.Modifiers == ModifierKeys.Shift)
        {
            // Shift+Enter would insert a break inside the paragraph: a line must stay a paragraph.
            e.Handled = true;
        }
    }

    /// <summary>Paste as plain text: clipboard formatting would break the split into lines and Runs.</summary>
    private void OnEditorPasting(object sender, DataObjectPastingEventArgs e)
    {
        if (e.SourceDataObject.GetData(DataFormats.UnicodeText, autoConvert: true) is not string text)
        {
            e.CancelCommand();
            return;
        }

        e.DataObject = new DataObject(DataFormats.UnicodeText, text);
        e.FormatToApply = DataFormats.UnicodeText;
    }

    private static void Restart(DispatcherTimer timer)
    {
        timer.Stop();
        timer.Start();
    }

    // ---------- Highlighting ----------

    private void Highlight()
    {
        if (_syntax is not { } syntax)
            return;

        // Redoing a line's Runs would lose an ongoing selection: wait until it is released.
        if (!Editor.Selection.IsEmpty)
        {
            Restart(_highlightTimer);
            return;
        }

        _isUpdating = true;
        _completion.IsSuspended = true;
        try
        {
            _highlighter.Apply(Editor, syntax);
        }
        finally
        {
            _completion.IsSuspended = false;
            _isUpdating = false;
        }
    }

    // ---------- Diagnostics ----------

    private async Task RefreshDiagnosticsAsync()
    {
        if (_service is not { } service)
            return;

        var version = ++_diagnosticVersion;
        var text = EditorText.GetText(Editor);
        IReadOnlyList<CodeDiagnostic> diagnostics;
        try
        {
            diagnostics = await Task.Run(() => service.GetDiagnosticsAsync(text));
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException or OperationCanceledException)
        {
            diagnostics = [];
        }

        // A more recent keystroke restarted the analysis.
        if (version != _diagnosticVersion)
            return;

        DiagnosticsList.ItemsSource = diagnostics.Select(d => d.Describe(text)).ToArray();
        DiagnosticsTab.Header = diagnostics.Count == 0 ? "Diagnostics" : $"Diagnostics ({diagnostics.Count})";
    }

    // ---------- Hover ----------

    private void OnEditorMouseMove(object sender, MouseEventArgs e)
    {
        _hoverPoint = e.GetPosition(Editor);
        Restart(_hoverTimer);
    }

    private async Task RefreshHoverAsync()
    {
        if (_service is not { } service || Editor.GetPositionFromPoint(_hoverPoint, snapToText: false) is not { } position)
            return;

        var text = EditorText.GetText(Editor);
        var offset = EditorText.GetOffset(Editor, position);
        try
        {
            var hover = await Task.Run(() => service.GetHoverAsync(text, offset));
            HoverText.Text = hover switch
            {
                null => string.Empty,
                { Description: null } => hover.Signature,
                _ => $"{hover.Signature}   —   {hover.Description.ReplaceLineEndings(" · ")}",
            };
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException or OperationCanceledException)
        {
            HoverText.Text = string.Empty;
        }
    }
}
