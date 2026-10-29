using Quill.Code.Analysis;
using Quill.Code.CSharp.Roslyn;

namespace Quill.Code.CSharp.Features;

/// <summary>Diagnostics Roslyn ramenés au texte de l'éditeur.</summary>
internal static class CSharpDiagnosticEngine
{
    public static async Task<IReadOnlyList<CodeDiagnostic>> GetDiagnosticsAsync(CSharpDocument document, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(document.Text))
            return [];

        // Le modèle sémantique rend aussi les fautes de syntaxe.
        if (document.Options.IncludeSemanticDiagnostics && await document.Document.GetSemanticModelAsync(cancellationToken) is { } semantic)
            return CSharpDiagnosticMapper.Map(semantic.GetDiagnostics(cancellationToken: cancellationToken), document.Text);

        return await document.Document.GetSyntaxTreeAsync(cancellationToken) is { } tree
            ? CSharpDiagnosticMapper.Map(tree.GetDiagnostics(cancellationToken), document.Text)
            : [];
    }
}
