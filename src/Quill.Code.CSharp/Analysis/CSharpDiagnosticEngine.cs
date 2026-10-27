using Microsoft.CodeAnalysis;
using Quill.Code.Analysis;
using Quill.Code.CSharp.Workspaces;
using System.Globalization;

namespace Quill.Code.CSharp.Analysis;

/// <summary>Diagnostics Roslyn ramenés au texte de l'éditeur.</summary>
internal static class CSharpDiagnosticEngine
{
    // Au-delà, une erreur en amont fait cascader les suivantes : les premières suffisent.
    private const int MaxDiagnostics = 100;

    public static async Task<IReadOnlyList<CodeDiagnostic>> GetDiagnosticsAsync(CSharpDocument document, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(document.Text) || await document.Document.GetSyntaxTreeAsync(cancellationToken) is not { } tree)
            return [];

        var syntax = tree.GetDiagnostics(cancellationToken).ToHashSet();
        IEnumerable<Diagnostic> all = syntax;
        if (document.IncludeSemanticDiagnostics && await document.Document.GetSemanticModelAsync(cancellationToken) is { } semantic)
            all = semantic.GetDiagnostics(cancellationToken: cancellationToken);

        return Map(all, syntax, document);
    }

    /// <param name="syntax">Fautes de syntaxe : seules celles-là sont rattachées à la fin du texte quand elles le dépassent.</param>
    private static List<CodeDiagnostic> Map(IEnumerable<Diagnostic> diagnostics, HashSet<Diagnostic> syntax, CSharpDocument document)
    {
        var text = document.Text;
        List<CodeDiagnostic> result = [];
        foreach (var diagnostic in diagnostics.OrderBy(d => d.Location.SourceSpan.Start))
        {
            if (diagnostic.IsSuppressed || diagnostic.Severity == DiagnosticSeverity.Hidden || !diagnostic.Location.IsInSource)
                continue;

            var span = diagnostic.Location.SourceSpan;
            var start = document.ToText(span.Start);
            if (start < 0)
                continue;

            if (start > text.Length)
            {
                // « ) » ou « ; » attendu est signalé sur le source qui suit le texte : rattaché à sa fin.
                // Une erreur sémantique au-delà du texte concerne le code généré lui-même.
                if (!syntax.Contains(diagnostic))
                    continue;

                start = text.Length;
            }

            var (visibleStart, visibleLength) = Widen(text, start, Math.Min(span.Length, text.Length - start));
            if (visibleLength == 0)
                continue;

            result.Add(new CodeDiagnostic(
                visibleStart,
                visibleLength,
                ToSeverity(diagnostic.Severity),
                diagnostic.GetMessage(CultureInfo.CurrentUICulture),
                diagnostic.Id));

            if (result.Count == MaxDiagnostics)
                break;
        }

        return result;
    }

    /// <summary>Un diagnostic sans longueur (symbole manquant) souligne le caractère visible le plus proche.</summary>
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
