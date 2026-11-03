using Quill.Code.Analysis;
using Quill.Code.Completion;
using Quill.Code.Constants;
using Quill.Code.CSharp.Linq.Compilation;
using Quill.Code.CSharp.Linq.Constants;
using Quill.Code.CSharp.Linq.Execution;
using Quill.Data.Sql;

namespace Quill.Code.CSharp.Linq;

/// <summary>
/// Services d'editeur d'une requete LINQ : <see cref="CSharpLanguageService"/> analysant le texte contre le DbContext
/// genere depuis la structure de la base (<c>Db.</c> → tables, <c>u.</c> → colonnes, <c>.Wh</c> → Where), les
/// parametres etant des variables. Le service C# est celui de <see cref="LinqQueryExecutor"/> : ce qui est souligne
/// est ce qui echoue a l'execution.
/// </summary>
public sealed class LinqLanguageService : ICodeLanguageService
{
    private readonly ILinqDatabaseProvider? _provider;
    private readonly Func<CancellationToken, Task<SqlDatabaseSchema?>>? _schema;
    private readonly IReadOnlyList<LinqParameter> _parameters;

    /// <summary>Service d'un editeur ; <paramref name="options"/> null : C# seul, sans parametre.</summary>
    public LinqLanguageService(LinqLanguageOptions? options = null)
    {
        _provider = options?.Provider;
        _schema = options?.Schema;

        // Sans les valeurs : le service Roslyn reste le meme d'une valeur a l'autre.
        _parameters = [.. (options?.Parameters ?? []).Select(p => LinqParameter.From(p) with { Value = null })];
    }

    /// <inheritdoc/>
    public string Language => CodeLanguages.CSharp;

    /// <inheritdoc/>
    public async Task<CompletionList> GetCompletionsAsync(string code, int position, CancellationToken cancellationToken = default)
        => await (await GetServiceAsync(cancellationToken).ConfigureAwait(false)).GetCompletionsAsync(code, position, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc/>
    public async Task<CompletionChange> GetChangeAsync(string code, CompletionItem item, CancellationToken cancellationToken = default)
        => await (await GetServiceAsync(cancellationToken).ConfigureAwait(false)).GetChangeAsync(code, item, cancellationToken).ConfigureAwait(false);

    /// <summary>Les informations (suggestions) ne sont pas soulignees, ni l'absence d'instruction (signalee a l'execution).</summary>
    public async Task<IReadOnlyList<CodeDiagnostic>> GetDiagnosticsAsync(string code, CancellationToken cancellationToken = default)
    {
        var diagnostics = await (await GetServiceAsync(cancellationToken).ConfigureAwait(false)).GetDiagnosticsAsync(code, cancellationToken).ConfigureAwait(false);
        return diagnostics.Where(d => d.Severity != CodeDiagnosticSeverity.Info && d.Code != LinqQueryConstants.NoEntryPointCode).ToArray();
    }

    /// <inheritdoc/>
    public async Task<HoverInfo?> GetHoverAsync(string code, int position, CancellationToken cancellationToken = default)
        => await (await GetServiceAsync(cancellationToken).ConfigureAwait(false)).GetHoverAsync(code, position, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc/>
    public async Task<SignatureHelpInfo?> GetSignatureHelpAsync(string code, int position, CancellationToken cancellationToken = default)
        => await (await GetServiceAsync(cancellationToken).ConfigureAwait(false)).GetSignatureHelpAsync(code, position, cancellationToken).ConfigureAwait(false);

    /// <summary>
    /// Composition MEF de Roslyn (1 a 2 s), JIT de la completion et compilation du DbContext genere, faits avant la
    /// premiere frappe ; n'echoue jamais.
    /// </summary>
    public async Task WarmUpAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var service = await GetServiceAsync(cancellationToken).ConfigureAwait(false);
            await service.WarmUpAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        }
#pragma warning disable CA1031 // Prechargement opportuniste : la vraie demande retentera et gerera l'erreur.
        catch (Exception)
#pragma warning restore CA1031
        {
        }
    }

    private async Task<CSharpLanguageService> GetServiceAsync(CancellationToken cancellationToken)
        => await LinqContexts.GetLanguageServiceAsync(_provider, await TryGetSchemaAsync(cancellationToken).ConfigureAwait(false), _parameters, cancellationToken).ConfigureAwait(false);

    /// <summary>Structure de la base, ou null : base injoignable, connexion fermee...</summary>
    private async Task<SqlDatabaseSchema?> TryGetSchemaAsync(CancellationToken cancellationToken)
    {
        if (_provider is null || _schema is null)
            return null;

        try
        {
            return await _schema(cancellationToken).ConfigureAwait(false);
        }
#pragma warning disable CA1031 // Comme ISqlCatalog : une exception de l'hote vaut absence de structure.
        catch (Exception ex) when (ex is not OperationCanceledException)
#pragma warning restore CA1031
        {
            return null;
        }
    }
}
