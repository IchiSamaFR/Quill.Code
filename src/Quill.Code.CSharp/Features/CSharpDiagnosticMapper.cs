using Microsoft.CodeAnalysis;
using Quill.Code.Analysis;
using Quill.Code.CSharp.Roslyn;
using System.Globalization;
using Quill.Code.CSharp.Constants;

namespace Quill.Code.CSharp.Features;

/// <summary>Roslyn diagnostics mapped back to the editor text (positions, severity, localized message).</summary>
internal static class CSharpDiagnosticMapper
{
    /// <summary>Diagnostics of a compilation (several documents): only those of the editor text are kept.</summary>
    public static async Task<IReadOnlyList<CodeDiagnostic>> MapAsync(CSharpDocument document, IEnumerable<Diagnostic> diagnostics, CancellationToken cancellationToken = default)
    {
        var tree = await document.Document.GetSyntaxTreeAsync(cancellationToken);
        return tree is null ? [] : Map(diagnostics.Where(d => d.Location.SourceTree == tree), document.Text);
    }

    public static IReadOnlyList<CodeDiagnostic> Map(IEnumerable<Diagnostic> diagnostics, string text)
    {
        List<CodeDiagnostic> result = [];
        foreach (var diagnostic in diagnostics.OrderBy(d => d.Location.SourceSpan.Start))
        {
            if (diagnostic.IsSuppressed || diagnostic.Severity == DiagnosticSeverity.Hidden || !diagnostic.Location.IsInSource)
                continue;

            var span = diagnostic.Location.SourceSpan;
            var start = Math.Min(span.Start, text.Length);
            var (visibleStart, visibleLength) = Widen(text, start, Math.Min(span.Length, text.Length - start));
            if (visibleLength == 0)
                continue;

            result.Add(new CodeDiagnostic(
                visibleStart,
                visibleLength,
                ToSeverity(diagnostic.Severity),
                diagnostic.GetMessage(CultureInfo.CurrentUICulture),
                diagnostic.Id));

            if (result.Count == CSharpDiagnosticConstants.MaxDiagnostics)
                break;
        }

        return result;
    }

    /// <summary>A zero-length diagnostic (";" expected at end of text…) underlines the nearest visible character.</summary>
    private static (int Start, int Length) Widen(string text, int start, int length)
    {
        if (length > 0)
            return (start, length);

        if (start < text.Length && !char.IsWhiteSpace(text[start]))
            return (start, 1);

        var previous = start - 1;
        while (previous >= 0 && char.IsWhiteSpace(text[previous]))
            previous--;

        return previous >= 0 ? (previous, 1) : (start, 0);
    }

    private static CodeDiagnosticSeverity ToSeverity(DiagnosticSeverity severity) => severity switch
    {
        DiagnosticSeverity.Error => CodeDiagnosticSeverity.Error,
        DiagnosticSeverity.Warning => CodeDiagnosticSeverity.Warning,
        _ => CodeDiagnosticSeverity.Info,
    };
}
