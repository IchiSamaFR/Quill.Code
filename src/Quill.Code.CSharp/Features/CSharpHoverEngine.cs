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

        // Infobulle de Visual Studio : libellés localisés, surcharges ; le nom touché par la souris compte aussi.
        var info = await service.GetQuickInfoAsync(document.Document, position, cancellationToken);
        var signature = info?.Sections.FirstOrDefault(section => section.Kind == QuickInfoSectionKinds.Description);
        if (info is null || signature is null)
            return null;

        // Un [Description] (table, colonne d'un modèle généré) prime sur la documentation XML.
        var symbol = await SymbolFinder.FindSymbolAtPositionAsync(document.Document, info.Span.Start, cancellationToken);
        var details = (symbol is null ? null : CSharpSymbolFormatter.Details(symbol))
            ?? info.Sections.FirstOrDefault(section => section.Kind == QuickInfoSectionKinds.DocumentationComments)?.Text;

        return new HoverInfo(info.Span.Start, info.Span.Length, signature.Text, details);
    }
}
