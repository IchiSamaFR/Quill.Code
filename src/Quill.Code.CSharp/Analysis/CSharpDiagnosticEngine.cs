using Microsoft.CodeAnalysis;
using Quill.Code.Analysis;
using Quill.Code.CSharp.Workspaces;

namespace Quill.Code.CSharp.Analysis;

/// <summary>Diagnostics Roslyn ramenés au texte de l'éditeur.</summary>
internal static class CSharpDiagnosticEngine
{
    public static async Task<IReadOnlyList<CodeDiagnostic>> GetDiagnosticsAsync(CSharpDocument document, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(document.Text) || await document.Document.GetSyntaxTreeAsync(cancellationToken) is not { } tree)
            return [];

        var syntax = tree.GetDiagnostics(cancellationToken).ToHashSet();
        IEnumerable<Diagnostic> all = syntax;
        if (document.IncludeSemanticDiagnostics && await document.Document.GetSemanticModelAsync(cancellationToken) is { } semantic)
            all = semantic.GetDiagnostics(cancellationToken: cancellationToken);

        return CSharpDiagnosticMapper.Map(all, syntax, document);
    }
}
