using System.Collections.Concurrent;
using System.Data.Common;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;
using Quill.Code.CSharp.Compilation;
using Quill.Code.Demo.Constants;
using Quill.Code.Demo.Editing;
using Quill.Code.Execution;
using Quill.Code.Scripting;

namespace Quill.Code.Demo;

/// <summary>Execution on the demo database: same options and same outputs for SQL and LINQ.</summary>
public partial class MainWindow
{
    private bool _isRunning;
    private CancellationTokenSource? _runCancellation;

    private async Task RunAsync()
    {
        if (_isRunning || _scenario is not { CanRun: true } scenario || _database.Schema is not { } schema)
            return;

        var script = Editor.Selection.IsEmpty ? EditorText.GetText(Editor) : Editor.Selection.Text;

        // Quill hands over messages and result sets as they come, from the execution thread.
        var logs = new ConcurrentQueue<LogEntry>();
        var resultSets = new ConcurrentQueue<QueryResultSet>();
        var options = new DataRunOptions { Log = logs.Enqueue, Data = resultSets.Enqueue };
        using var cancellation = new CancellationTokenSource();
        _runCancellation = cancellation;

        _isRunning = true;
        RunButton.IsEnabled = false;
        StopButton.IsEnabled = true;
        MessagesBox.Text = "Running…";
        try
        {
            await Task.Run(() => scenario.IsLinq
                ? _database.LinqExecutor.RunAsync(_database.ConnectionString, schema, script, options, cancellation.Token)
                : _database.SqlExecutor.RunAsync(_database.ConnectionString, script, options, cancellation.Token));
        }
        catch (CSharpRunAbandonedException ex)
        {
            logs.Enqueue(new LogEntry(LogLevel.Error, ex.Message));
        }
        catch (Exception ex) when (ex is DbException or InvalidOperationException or OperationCanceledException)
        {
            logs.Enqueue(new LogEntry(LogLevel.Error, ex.Message));
        }
        finally
        {
            _isRunning = false;
            _runCancellation = null;
            StopButton.IsEnabled = false;
            RunButton.IsEnabled = _scenario is { CanRun: true };
        }

        ShowResults([.. resultSets], [.. logs]);
    }

    private void ShowResults(IReadOnlyList<QueryResultSet> resultSets, IReadOnlyList<LogEntry> logs)
    {
        foreach (var tab in ResultTabs.Items.OfType<TabItem>().Where(t => Equals(t.Tag, DemoConstants.ResultTabTag)).ToArray())
            ResultTabs.Items.Remove(tab);

        for (var i = 0; i < resultSets.Count; i++)
        {
            var resultSet = resultSets[i];
            ResultTabs.Items.Insert(i, new TabItem
            {
                // A TextBlock: a plain-text header would take a "_" for an access key.
                Header = new TextBlock { Text = string.Create(CultureInfo.InvariantCulture, $"{resultSet.Title} ({resultSet.Rows.Count})") },
                Tag = DemoConstants.ResultTabTag,
                Content = CreateGrid(resultSet),
            });
        }

        MessagesBox.Text = string.Join(Environment.NewLine, logs.Select(l => string.Create(CultureInfo.InvariantCulture, $"{l.Timestamp:HH:mm:ss}  {l.Level,-11}  {l.Text}")));
        ResultTabs.SelectedItem = resultSets.Count > 0 ? ResultTabs.Items[0] : MessagesTab;
    }

    /// <summary>The cells of a result set are text, read by position.</summary>
    private static DataGrid CreateGrid(QueryResultSet resultSet)
    {
        var grid = new DataGrid
        {
            AutoGenerateColumns = false,
            IsReadOnly = true,
            HeadersVisibility = DataGridHeadersVisibility.Column,
            ItemsSource = resultSet.Rows,
        };

        for (var i = 0; i < resultSet.Columns.Count; i++)
        {
            grid.Columns.Add(new DataGridTextColumn
            {
                Header = new TextBlock { Text = resultSet.Columns[i].Name },
                Binding = new Binding(string.Create(CultureInfo.InvariantCulture, $"Cells[{i}]")),
            });
        }

        return grid;
    }
}
