using Quill.Code.Analysis;
using Quill.Code.CSharp.Roslyn;

namespace Quill.Code.CSharp.Features;

/// <summary>Roslyn diagnostics mapped back to the editor text.</summary>
internal static class CSharpDiagnosticEngine
{
    public static async Task<IReadOnlyList<CodeDiagnostic>> GetDiagnosticsAsync(CSharpDocument document, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(document.Text))
            return [];

        // The semantic model also returns syntax errors.
        if (document.Options.IncludeSemanticDiagnostics && await document.Document.GetSemanticModelAsync(cancellationToken) is { } semantic)
            return CSharpDiagnosticMapper.Map(semantic.GetDiagnostics(cancellationToken: cancellationToken), document.Text);

        return await document.Document.GetSyntaxTreeAsync(cancellationToken) is { } tree
            ? CSharpDiagnosticMapper.Map(tree.GetDiagnostics(cancellationToken), document.Text)
            : [];
    }
}
