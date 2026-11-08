using Quill.Code.Analysis;
using Quill.Code.Completion;
using Quill.Code.Constants;
using Quill.Code.CSharp.Linq.Compilation;
using Quill.Code.CSharp.Linq.Constants;
using Quill.Code.CSharp.Linq.Execution;
using Quill.Data.Sql;

namespace Quill.Code.CSharp.Linq;

/// <summary>
/// Editor services for a LINQ query: <see cref="CSharpLanguageService"/> analyzing the text against the DbContext
/// generated from the database structure (<c>Db.</c> → tables, <c>u.</c> → columns, <c>.Wh</c> → Where), the
/// parameters being variables. The C# service is the one of <see cref="LinqQueryExecutor"/>: what is underlined
/// is what fails at execution.
/// </summary>
public sealed class LinqLanguageService : ICodeLanguageService
{
    private readonly ILinqDatabaseProvider? _provider;
    private readonly Func<CancellationToken, Task<SqlDatabaseSchema?>>? _schema;
    private readonly IReadOnlyList<LinqParameter> _parameters;

    /// <summary>Service of an editor; <paramref name="options"/> null: C# only, no parameters.</summary>
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

    /// <summary>Information items (suggestions) are not underlined, nor is a missing statement (reported at execution).</summary>
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
    /// Roslyn MEF composition (1 to 2 s), completion JIT and compilation of the generated DbContext, done before the
    /// first keystroke; never fails.
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

    /// <summary>Database structure, or null: unreachable database, closed connection...</summary>
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
