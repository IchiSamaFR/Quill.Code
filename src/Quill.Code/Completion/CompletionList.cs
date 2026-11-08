namespace Quill.Code.Completion;

/// <summary>Result of a completion request.</summary>
/// <param name="Items">Suggestions, in the provider's sort order.</param>
/// <param name="SpanStart">Start of the span the committed suggestion will replace (word being typed).</param>
/// <param name="SpanLength">Length of that span at the time of the request.</param>
/// <param name="IsSuggestionMode">
/// True when the user is naming something (declaration): no suggestion should be committed automatically.
/// </param>
public sealed record CompletionList(IReadOnlyList<CompletionItem> Items, int SpanStart, int SpanLength, bool IsSuggestionMode = false)
{
    /// <summary>Empty list.</summary>
    public static CompletionList Empty { get; } = new([], 0, 0);

    /// <summary>End of the replaced span.</summary>
    public int SpanEnd => SpanStart + SpanLength;
}
