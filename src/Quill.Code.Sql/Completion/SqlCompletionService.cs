using Quill.Code.Completion;

namespace Quill.Code.Sql.Completion;

/// <summary>
/// Autocomplétion T-SQL par défaut : aucune proposition. Tables, colonnes et procédures demandent le schéma de la
/// base, que Quill ne connaît pas ; l'hôte fournit sa propre <see cref="ICodeCompletion"/> (appuyée sur
/// <see cref="SqlContextAnalyzer"/> pour savoir ce qui est attendu au curseur).
/// </summary>
public sealed class SqlCompletionService : ICodeCompletion
{
    /// <inheritdoc />
    public Task<CompletionList> GetCompletionsAsync(string code, int position, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(code);

        return Task.FromResult(CompletionList.Empty);
    }

    /// <inheritdoc />
    public Task<CompletionChange> GetChangeAsync(string code, CompletionItem item, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(code);
        ArgumentNullException.ThrowIfNull(item);

        // Aucune proposition n'est faite par ce service : il n'y a rien à valider.
        return Task.FromResult(new CompletionChange(0, 0, string.Empty));
    }
}
