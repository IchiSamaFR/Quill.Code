namespace Quill.Code.Execution;

/// <summary>A result set (one SELECT) returned by the execution.</summary>
public sealed class QueryResultSet
{
    /// <summary>Position of the set in the execution (1-based).</summary>
    public int Index { get; }

    /// <summary>Columns, in server order.</summary>
    public IReadOnlyList<QueryResultColumn> Columns { get; }

    /// <summary>Rows read, at most the requested limit.</summary>
    public IReadOnlyList<QueryResultRow> Rows { get; }

    /// <summary>True if reading stopped at the row limit.</summary>
    public bool IsTruncated { get; }

    private readonly string? _title;

    /// <summary>Tab title: the one given at execution (Dump("...")), otherwise "Résultats n".</summary>
    public string Title => string.IsNullOrWhiteSpace(_title) ? $"Résultats {Index}" : _title;

    /// <summary>Number of rows read, for the status bar.</summary>
    public string Summary => IsTruncated
        ? $"{Rows.Count} premières lignes"
        : $"{Rows.Count} ligne(s)";

    /// <summary>Result set; <paramref name="title"/> replaces "Résultats n".</summary>
    public QueryResultSet(int index, IReadOnlyList<QueryResultColumn> columns, IReadOnlyList<QueryResultRow> rows, bool isTruncated, string? title = null)
    {
        Index = index;
        _title = title;
        Columns = columns;
        Rows = rows;
        IsTruncated = isTruncated;
    }
}
