using Quill.Code.Analysis;
using Quill.Code.Completion;
using Quill.Code.CSharp.Analysis;
using Quill.Code.CSharp.Completion;
using Quill.Code.CSharp.Workspaces;

namespace Quill.Code.CSharp;

/// <inheritdoc cref="ICSharpLanguageService"/>
/// <remarks>
/// Sans état : les documents sont des instantanés immuables, les demandes peuvent être concurrentes.
/// Roslyn calcule en partie de façon synchrone : chaque demande part sur le pool de threads pour ne pas figer l'interface.
/// </remarks>
public sealed class CSharpLanguageService : ICSharpLanguageService
{
    /// <inheritdoc />
    public Task WarmUpAsync(CSharpDocument document, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);
        return GetCompletionsAsync(document, document.Text.Length, cancellationToken);
    }

    /// <inheritdoc />
    public Task<CompletionList> GetCompletionsAsync(CSharpDocument document, int position, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);
        return Task.Run(() => CSharpCompletionEngine.GetCompletionsAsync(document, position, cancellationToken), cancellationToken);
    }

    /// <inheritdoc />
    public Task<CompletionChange> GetChangeAsync(CSharpDocument document, CompletionItem item, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(item);
        return Task.Run(() => CSharpCompletionEngine.GetChangeAsync(document, item, cancellationToken), cancellationToken);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<CodeDiagnostic>> GetDiagnosticsAsync(CSharpDocument document, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);
        return Task.Run(() => CSharpDiagnosticEngine.GetDiagnosticsAsync(document, cancellationToken), cancellationToken);
    }

    /// <inheritdoc />
    public Task<HoverInfo?> GetHoverAsync(CSharpDocument document, int position, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);
        return Task.Run(() => CSharpHoverEngine.GetHoverAsync(document, position, cancellationToken), cancellationToken);
    }

    /// <inheritdoc />
    public Task<SignatureHelpInfo?> GetSignatureHelpAsync(CSharpDocument document, int position, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);
        return Task.Run(() => CSharpSignatureHelpEngine.GetSignatureHelpAsync(document, position, cancellationToken), cancellationToken);
    }
}
