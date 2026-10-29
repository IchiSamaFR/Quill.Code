using Quill.Code.Completion;
using Quill.Code.CSharp.Workspaces;

namespace Quill.Code.CSharp.Completion;

/// <summary>Autocomplétion C# fournie par Roslyn.</summary>
/// <remarks>
/// Les méthodes qui prennent le code créent le document depuis le projet (transformation, références du texte) ;
/// celles qui prennent un document travaillent sur n'importe quel projet. Roslyn calcule en partie de façon
/// synchrone : chaque demande part sur le pool de threads pour ne pas figer l'interface.
/// </remarks>
/// <param name="project">Projet des demandes sur le code.</param>
public sealed class CSharpCompletionService(CSharpProject project) : ICodeCompletion
{
    private readonly CSharpProject _project = project ?? throw new ArgumentNullException(nameof(project));

    /// <inheritdoc />
    public Task<CompletionList> GetCompletionsAsync(string code, int position, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(code);
        return Task.Run(() => CSharpCompletionEngine.GetCompletionsAsync(_project.CreateDocument(code), position, cancellationToken), cancellationToken);
    }

    /// <inheritdoc />
    public Task<CompletionChange> GetChangeAsync(string code, CompletionItem item, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(code);
        ArgumentNullException.ThrowIfNull(item);
        return Task.Run(() => CSharpCompletionEngine.GetChangeAsync(_project.CreateDocument(code), item, cancellationToken), cancellationToken);
    }

    /// <summary>Propositions valables à <paramref name="position"/> dans <paramref name="document"/>.</summary>
    public Task<CompletionList> GetCompletionsAsync(CSharpDocument document, int position, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);
        return Task.Run(() => CSharpCompletionEngine.GetCompletionsAsync(document, position, cancellationToken), cancellationToken);
    }

    /// <summary>Modification à appliquer pour valider <paramref name="item"/> dans <paramref name="document"/>.</summary>
    public Task<CompletionChange> GetChangeAsync(CSharpDocument document, CompletionItem item, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(item);
        return Task.Run(() => CSharpCompletionEngine.GetChangeAsync(document, item, cancellationToken), cancellationToken);
    }
}
