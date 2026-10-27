using Quill.Code.Analysis;
using Quill.Code.Completion;
using Quill.Code.CSharp.Workspaces;

namespace Quill.Code.CSharp;

/// <summary>
/// Services d'éditeur sur le texte brut : le document est créé par Quill depuis le <see cref="CSharpProject"/>
/// (transformation, références du texte), le tout hors du thread de l'interface.
/// </summary>
public static class CSharpLanguageServiceExtensions
{
    /// <inheritdoc cref="ICSharpLanguageService.WarmUpAsync"/>
    /// <param name="code">Texte de préchauffage ; « Console. » prépare aussi la recherche des membres.</param>
    public static Task WarmUpAsync(this ICSharpLanguageService service, CSharpProject project, string code = "", CancellationToken cancellationToken = default)
        => RunWithoutResult(service, project, code, (s, document) => s.WarmUpAsync(document, cancellationToken), cancellationToken);

    /// <inheritdoc cref="ICSharpLanguageService.GetCompletionsAsync"/>
    public static Task<CompletionList> GetCompletionsAsync(this ICSharpLanguageService service, CSharpProject project, string code, int position, CancellationToken cancellationToken = default)
        => Run(service, project, code, (s, document) => s.GetCompletionsAsync(document, position, cancellationToken), cancellationToken);

    /// <inheritdoc cref="ICSharpLanguageService.GetChangeAsync"/>
    public static Task<CompletionChange> GetChangeAsync(this ICSharpLanguageService service, CSharpProject project, string code, CompletionItem item, CancellationToken cancellationToken = default)
        => Run(service, project, code, (s, document) => s.GetChangeAsync(document, item, cancellationToken), cancellationToken);

    /// <inheritdoc cref="ICSharpLanguageService.GetDiagnosticsAsync"/>
    public static Task<IReadOnlyList<CodeDiagnostic>> GetDiagnosticsAsync(this ICSharpLanguageService service, CSharpProject project, string code, CancellationToken cancellationToken = default)
        => Run(service, project, code, (s, document) => s.GetDiagnosticsAsync(document, cancellationToken), cancellationToken);

    /// <inheritdoc cref="ICSharpLanguageService.GetHoverAsync"/>
    public static Task<HoverInfo?> GetHoverAsync(this ICSharpLanguageService service, CSharpProject project, string code, int position, CancellationToken cancellationToken = default)
        => Run(service, project, code, (s, document) => s.GetHoverAsync(document, position, cancellationToken), cancellationToken);

    /// <inheritdoc cref="ICSharpLanguageService.GetSignatureHelpAsync"/>
    public static Task<SignatureHelpInfo?> GetSignatureHelpAsync(this ICSharpLanguageService service, CSharpProject project, string code, int position, CancellationToken cancellationToken = default)
        => Run(service, project, code, (s, document) => s.GetSignatureHelpAsync(document, position, cancellationToken), cancellationToken);

    // Création du document comprise : premier accès au projet (MEF), transformation et références du texte.
    private static Task RunWithoutResult(ICSharpLanguageService service, CSharpProject project, string code, Func<ICSharpLanguageService, CSharpDocument, Task> call, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(code);

        return Task.Run(() => call(service, project.CreateDocument(code)), cancellationToken);
    }

    private static Task<T> Run<T>(ICSharpLanguageService service, CSharpProject project, string code, Func<ICSharpLanguageService, CSharpDocument, Task<T>> call, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(code);

        return Task.Run(() => call(service, project.CreateDocument(code)), cancellationToken);
    }
}
