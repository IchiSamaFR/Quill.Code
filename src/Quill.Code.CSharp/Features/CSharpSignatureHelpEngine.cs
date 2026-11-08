using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Quill.Code.Analysis;
using Quill.Code.CSharp.Roslyn;

namespace Quill.Code.CSharp.Features;

/// <summary>Overloads of the method whose argument list contains the caret.</summary>
internal static class CSharpSignatureHelpEngine
{
    public static async Task<SignatureHelpInfo?> GetSignatureHelpAsync(CSharpDocument document, int position, CancellationToken cancellationToken)
    {
        if (position <= 0 || position > document.Text.Length || await document.Document.GetSyntaxRootAsync(cancellationToken) is not { } root)
            return null;

        var argumentList = FindArgumentList(root, position);
        if (argumentList?.Parent is not InvocationExpressionSyntax invocation)
            return null;

        if (await document.Document.GetSemanticModelAsync(cancellationToken) is not { } semantic)
            return null;

        // Toutes les surcharges visibles (méthodes d'extension comprises) ; à défaut, les candidates d'un appel non résolu.
        var resolved = semantic.GetSymbolInfo(invocation, cancellationToken);
        var methods = semantic.GetMemberGroup(invocation.Expression, cancellationToken).OfType<IMethodSymbol>().ToList();
        if (methods.Count == 0)
            methods = resolved.CandidateSymbols.OfType<IMethodSymbol>().ToList();

        if (methods.Count == 0)
            return null;

        var activeParameter = argumentList.Arguments.GetSeparators().Count(separator => separator.SpanStart < position);
        var activeSignature = resolved.Symbol is IMethodSymbol chosen ? methods.FindIndex(m => IsSameMethod(m, chosen)) : -1;
        if (activeSignature < 0)
            activeSignature = Math.Max(0, methods.FindIndex(m => m.Parameters.Length > activeParameter || m.Parameters.LastOrDefault()?.IsParams == true));

        return new SignatureHelpInfo(
            methods.Select(CSharpSymbolFormatter.ToSignature).ToArray(),
            activeSignature,
            activeParameter,
            argumentList.OpenParenToken.SpanStart);
    }

    // Même méthode, qu'elle soit vue comme extension réduite (x.Where) ou statique (Queryable.Where).
    private static bool IsSameMethod(IMethodSymbol left, IMethodSymbol right)
        => SymbolEqualityComparer.Default.Equals((left.ReducedFrom ?? left).OriginalDefinition, (right.ReducedFrom ?? right).OriginalDefinition);

    /// <summary>Innermost argument list whose parentheses contain the caret.</summary>
    private static ArgumentListSyntax? FindArgumentList(SyntaxNode root, int position)
        => root.FindToken(position - 1).Parent?
            .AncestorsAndSelf()
            .OfType<ArgumentListSyntax>()
            .FirstOrDefault(list => list.OpenParenToken.Span.End <= position
                && (list.CloseParenToken.IsMissing || position <= list.CloseParenToken.SpanStart));
}
