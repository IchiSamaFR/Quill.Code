using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Quill.Code.Completion;
using Quill.Code.CSharp.Workspaces;
using RoslynCompletion = Microsoft.CodeAnalysis.Completion;

namespace Quill.Code.CSharp.Completion;

/// <summary>Autocomplétion C# de Roslyn, ramenée au texte de l'éditeur.</summary>
internal static class CSharpCompletionEngine
{
    public static async Task<CompletionList> GetCompletionsAsync(CSharpDocument document, int position, CancellationToken cancellationToken)
    {
        if (RoslynCompletion.CompletionService.GetService(document.Document) is not { } service)
            return CompletionList.Empty;

        var list = await service.GetCompletionsAsync(document.Document, document.ToSource(position), cancellationToken: cancellationToken);
        if (list.ItemsList.Count == 0)
            return CompletionList.Empty;

        var imported = document.AllowImports ? null : await ImportedNamespacesAsync(document.Document, cancellationToken);

        // Toutes les propositions : le filtre de saisie de l'éditeur les réduit à la frappe.
        List<CompletionItem> items = new(list.ItemsList.Count);
        foreach (var item in list.ItemsList)
        {
            var kind = CompletionKindMapper.FromTags(item.Tags);
            var insertText = imported is null ? item.DisplayText : QualifiedInsertText(item, kind, imported);
            if (insertText is null)
                continue;

            items.Add(new CompletionItem
            {
                Label = item.DisplayTextPrefix + item.DisplayText + item.DisplayTextSuffix,
                Kind = kind,
                InsertText = insertText,
                FilterText = item.FilterText,
                Detail = string.IsNullOrEmpty(item.InlineDescription) ? null : item.InlineDescription,
                Data = item,
            });
        }

        var spanStart = Math.Max(0, document.ToText(list.Span.Start));
        return new CompletionList(items, spanStart, list.Span.Length, list.SuggestionModeItem is not null);
    }

    public static async Task<CompletionChange> GetChangeAsync(CSharpDocument document, CompletionItem item, CancellationToken cancellationToken)
    {
        if (item.Data is not RoslynCompletion.CompletionItem roslynItem)
            throw new ArgumentException("La proposition ne vient pas de l'autocomplétion C#.", nameof(item));

        // Sans import possible, le texte calculé (nom complet si besoin) remplace simplement le mot.
        if (!document.AllowImports || RoslynCompletion.CompletionService.GetService(document.Document) is not { } service)
            return Replace(document, roslynItem.Span, item.TextToInsert);

        var change = await service.GetChangeAsync(document.Document, roslynItem, cancellationToken: cancellationToken);
        return await ToEditorChangeAsync(document, change, cancellationToken)
            ?? Replace(document, roslynItem.Span, item.TextToInsert);
    }

    /// <summary>
    /// Le source peut différer du texte de l'éditeur (directives neutralisées) : le changement de Roslyn est ramené
    /// à un diff fin, rejoué sur le texte réel, dont on prend l'enveloppe. Null s'il touche le source hors du texte.
    /// </summary>
    private static async Task<CompletionChange?> ToEditorChangeAsync(CSharpDocument document, RoslynCompletion.CompletionChange change, CancellationToken cancellationToken)
    {
        var source = await document.Document.GetTextAsync(cancellationToken);
        var caret = change.NewPosition is int newPosition ? document.ToText(newPosition) : (int?)null;

        var changes = source.WithChanges(change.TextChange).GetTextChanges(source);
        if (changes.Count == 0)
            return new CompletionChange(Math.Max(0, document.ToText(change.TextChange.Span.Start)), 0, string.Empty, caret);

        List<TextChange> shifted = new(changes.Count);
        foreach (var c in changes)
        {
            var start = document.ToText(c.Span.Start);
            if (start < 0 || start + c.Span.Length > document.Text.Length)
                return null;

            shifted.Add(new TextChange(new TextSpan(start, c.Span.Length), c.NewText ?? string.Empty));
        }

        // Positions croissantes et sans chevauchement : rejouées telles quelles sur le texte réel.
        var spanStart = shifted.Min(c => c.Span.Start);
        var spanEnd = shifted.Max(c => c.Span.End);
        var applied = SourceText.From(document.Text).WithChanges(shifted).ToString();
        var replacement = applied.Substring(spanStart, spanEnd - spanStart + (applied.Length - document.Text.Length));

        return new CompletionChange(spanStart, spanEnd - spanStart, replacement, caret);
    }

    private static CompletionChange Replace(CSharpDocument document, TextSpan span, string text)
    {
        var start = Math.Clamp(document.ToText(span.Start), 0, document.Text.Length);
        var length = Math.Clamp(span.Length, 0, document.Text.Length - start);
        return new CompletionChange(start, length, text);
    }

    /// <summary>Usings du document : ce qui en vient s'écrit sans qualification.</summary>
    private static async Task<HashSet<string>> ImportedNamespacesAsync(Document document, CancellationToken cancellationToken)
        => await document.GetSyntaxRootAsync(cancellationToken) is CompilationUnitSyntax root
            ? root.Usings.Select(u => u.Name?.ToString()).OfType<string>().ToHashSet(StringComparer.Ordinal)
            : [];

    /// <summary>
    /// Élément d'un espace de noms non importé (Roslyn le met dans InlineDescription) : un type est inséré avec son
    /// nom complet ; une méthode ne se qualifie pas après un point, elle ne compilerait pas : écartée (null).
    /// </summary>
    private static string? QualifiedInsertText(RoslynCompletion.CompletionItem item, CompletionItemKind kind, HashSet<string> imported)
    {
        var ns = item.InlineDescription;
        if (string.IsNullOrEmpty(ns) || imported.Contains(ns))
            return item.DisplayText;

        if (kind.IsCallable())
            return null;

        return kind.IsType() ? $"{ns}.{item.DisplayText}" : item.DisplayText;
    }
}
