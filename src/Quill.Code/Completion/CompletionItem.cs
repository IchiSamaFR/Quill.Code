namespace Quill.Code.Completion;

/// <summary>Completion suggestion.</summary>
public sealed record CompletionItem
{
    /// <summary>Text shown in the list (e.g. "Where&lt;&gt;").</summary>
    public required string Label { get; init; }

    /// <summary>Kind of the suggested symbol.</summary>
    public required CompletionItemKind Kind { get; init; }

    /// <summary>Text inserted on commit; the label by default.</summary>
    public string? InsertText { get; init; }

    /// <summary>Text compared with the input for filtering; the label by default.</summary>
    public string? FilterText { get; init; }

    /// <summary>Detail shown next to the label (column type, namespace…).</summary>
    public string? Detail { get; init; }

    /// <summary>Provider-specific object (Roslyn item…), returned as is on commit.</summary>
    public object? Data { get; init; }

    /// <summary>Text to insert: <see cref="InsertText"/>, otherwise <see cref="Label"/>.</summary>
    public string TextToInsert => InsertText ?? Label;

    /// <summary>Text to filter: <see cref="FilterText"/>, otherwise <see cref="Label"/>.</summary>
    public string TextToFilter => FilterText ?? Label;
}
