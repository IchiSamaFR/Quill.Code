using Quill.Code.Analysis;
using Quill.Code.Completion;
using Quill.Code.Languages;
using Quill.Code.Sql.Analysis;

namespace Quill.Code.Sql;

/// <summary>
/// Services d'éditeur T-SQL. Les diagnostics de syntaxe sont complets ; l'autocomplétion, le survol et l'aide à la
/// signature demandent le schéma de la base (tables, colonnes), que ce projet ne connaît pas : ils ne proposent rien
/// tant qu'un hôte ne les enrichit pas (voir <see cref="SqlContextAnalyzer"/> pour savoir ce qui est attendu au curseur).
/// </summary>
public sealed class SqlLanguageService : ICodeLanguageProvider
{
    /// <inheritdoc />
    public string Language => CodeLanguages.Sql;

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

        // Ce service ne propose rien : il n'y a aucune proposition à valider, donc aucune modification.
        return Task.FromResult(new CompletionChange(0, 0, string.Empty));
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<CodeDiagnostic>> GetDiagnosticsAsync(string code, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(code);

        return Task.FromResult(SqlSyntaxChecker.Check(code));
    }

    /// <inheritdoc />
    public Task<HoverInfo?> GetHoverAsync(string code, int position, CancellationToken cancellationToken = default)
        => Task.FromResult<HoverInfo?>(null);

    /// <inheritdoc />
    public Task<SignatureHelpInfo?> GetSignatureHelpAsync(string code, int position, CancellationToken cancellationToken = default)
        => Task.FromResult<SignatureHelpInfo?>(null);
}
