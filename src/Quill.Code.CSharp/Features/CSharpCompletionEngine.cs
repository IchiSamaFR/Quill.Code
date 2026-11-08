using Microsoft.CodeAnalysis.Text;
using Quill.Code.Completion;
using Quill.Code.CSharp.Roslyn;
using RoslynCompletion = Microsoft.CodeAnalysis.Completion;

namespace Quill.Code.CSharp.Features;

/// <summary>Roslyn C# completion, mapped back to the editor text.</summary>
internal static class CSharpCompletionEngine
{
    public static async Task<CompletionList> GetCompletionsAsync(CSharpDocument document, int position, CancellationToken cancellationToken)
    {
        if (RoslynCompletion.CompletionService.GetService(document.Document) is not { } service)
            return CompletionList.Empty;

        var list = await service.GetCompletionsAsync(document.Document, document.Clamp(position), cancellationToken: cancellationToken);
        if (list.ItemsList.Count == 0)
            return CompletionList.Empty;

        // Toutes les propositions : le filtre de saisie de l'éditeur les réduit à la frappe.
        List<CompletionItem> items = new(list.ItemsList.Count);
        foreach (var item in list.ItemsList)
        {
            items.Add(new CompletionItem
            {
                Label = item.DisplayTextPrefix + item.DisplayText + item.DisplayTextSuffix,
                Kind = CompletionKindMapper.FromTags(item.Tags),
                InsertText = item.DisplayText,
                FilterText = item.FilterText,
                Detail = string.IsNullOrEmpty(item.InlineDescription) ? null : item.InlineDescription,
                Data = item,
            });
        }

        return new CompletionList(items, list.Span.Start, list.Span.Length, list.SuggestionModeItem is not null);
    }

    public static async Task<CompletionChange> GetChangeAsync(CSharpDocument document, CompletionItem item, CancellationToken cancellationToken)
    {
        if (item.Data is not RoslynCompletion.CompletionItem roslynItem)
            throw new ArgumentException("La proposition ne vient pas de l'autocomplétion C#.", nameof(item));

        if (RoslynCompletion.CompletionService.GetService(document.Document) is not { } service)
            return Replace(document, roslynItem.Span, item.TextToInsert);

        var change = await service.GetChangeAsync(document.Document, roslynItem, cancellationToken: cancellationToken);
        return await ToEditorChangeAsync(document, change, cancellationToken)
            ?? Replace(document, roslynItem.Span, item.TextToInsert);
    }

    /// <summary>
    /// The source may differ from the editor text (neutralized directives): Roslyn's change is reduced
    /// to a fine-grained diff, replayed on the real text, whose envelope is taken. Null if it falls outside the text.
    /// </summary>
    private static async Task<CompletionChange?> ToEditorChangeAsync(CSharpDocument document, RoslynCompletion.CompletionChange change, CancellationToken cancellationToken)
    {
        var source = await document.Document.GetTextAsync(cancellationToken);

        var changes = source.WithChanges(change.TextChange).GetTextChanges(source);
        if (changes.Count == 0)
            return new CompletionChange(change.TextChange.Span.Start, 0, string.Empty, change.NewPosition);

        if (changes.Any(c => c.Span.End > document.Text.Length))
            return null;

        // Positions croissantes et sans chevauchement : rejouées telles quelles sur le texte réel.
        var spanStart = changes.Min(c => c.Span.Start);
        var spanEnd = changes.Max(c => c.Span.End);
        var applied = SourceText.From(document.Text).WithChanges(changes).ToString();
        var replacement = applied.Substring(spanStart, spanEnd - spanStart + (applied.Length - document.Text.Length));

        return new CompletionChange(spanStart, spanEnd - spanStart, replacement, change.NewPosition);
    }

    private static CompletionChange Replace(CSharpDocument document, TextSpan span, string text)
    {
        var start = document.Clamp(span.Start);
        var length = Math.Clamp(span.Length, 0, document.Text.Length - start);
        return new CompletionChange(start, length, text);
    }
}
