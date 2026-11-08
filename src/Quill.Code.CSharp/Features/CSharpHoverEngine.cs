using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.QuickInfo;
using Quill.Code.Analysis;
using Quill.Code.CSharp.Roslyn;

namespace Quill.Code.CSharp.Features;

/// <summary>Tooltip of the hovered name, computed by Roslyn's QuickInfo.</summary>
internal static class CSharpHoverEngine
{
    public static async Task<HoverInfo?> GetHoverAsync(CSharpDocument document, int position, CancellationToken cancellationToken)
    {
        if (position < 0 || position > document.Text.Length || QuickInfoService.GetService(document.Document) is not { } service)
            return null;

        // Visual Studio tooltip: localized labels, overloads; the name touched by the mouse counts too.
        var info = await service.GetQuickInfoAsync(document.Document, position, cancellationToken);
        var signature = info?.Sections.FirstOrDefault(section => section.Kind == QuickInfoSectionKinds.Description);
        if (info is null || signature is null)
            return null;

        // A [Description] (table, column of a generated model) takes precedence over the XML documentation.
        var symbol = await SymbolFinder.FindSymbolAtPositionAsync(document.Document, info.Span.Start, cancellationToken);
        var details = (symbol is null ? null : CSharpSymbolFormatter.Details(symbol))
            ?? info.Sections.FirstOrDefault(section => section.Kind == QuickInfoSectionKinds.DocumentationComments)?.Text;

        return new HoverInfo(info.Span.Start, info.Span.Length, signature.Text, details);
    }
}
