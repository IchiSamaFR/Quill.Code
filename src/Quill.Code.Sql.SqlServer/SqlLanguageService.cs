using Quill.Code.Analysis;
using Quill.Code.Completion;
using Quill.Code.Constants;
using Quill.Code.Sql.SqlServer.Analysis;
using Quill.Code.Sql.SqlServer.Completion;

namespace Quill.Code.Sql.SqlServer;

/// <summary>
/// Services d'éditeur T-SQL : fautes de syntaxe (ScriptDom), autocomplétion et survol. Sans catalogue, les propositions
/// se limitent aux mots-clés, aux mots du document et aux variables ; avec <see cref="SqlLanguageOptions.Catalog"/>,
/// bases, schémas, tables, vues, procédures et colonnes s'y ajoutent.
/// </summary>
public sealed class SqlLanguageService : ICodeLanguageService
{
    private readonly SqlCompletionService _completion;
    private readonly SqlDiagnosticService _diagnostic;
    private readonly SqlHoverService _hover;
    private readonly SqlSignatureService _signature = new();

    /// <param name="options">Catalogue, base et variables connus ; à défaut, aucun.</param>
    public SqlLanguageService(SqlLanguageOptions? options = null)
    {
        options ??= new SqlLanguageOptions();
        _completion = new SqlCompletionService(options);
        _diagnostic = new SqlDiagnosticService(options);
        _hover = new SqlHoverService(options);
    }

    /// <inheritdoc />
    public string Language => CodeLanguages.Sql;

    /// <inheritdoc />
    public Task<CompletionList> GetCompletionsAsync(string code, int position, CancellationToken cancellationToken = default)
        => _completion.GetCompletionsAsync(code, position, cancellationToken);

    /// <inheritdoc />
    public Task<CompletionChange> GetChangeAsync(string code, CompletionItem item, CancellationToken cancellationToken = default)
        => _completion.GetChangeAsync(code, item, cancellationToken);

    /// <inheritdoc />
    public Task<IReadOnlyList<CodeDiagnostic>> GetDiagnosticsAsync(string code, CancellationToken cancellationToken = default)
        => _diagnostic.GetDiagnosticsAsync(code, cancellationToken);

    /// <inheritdoc />
    public Task<HoverInfo?> GetHoverAsync(string code, int position, CancellationToken cancellationToken = default)
        => _hover.GetHoverAsync(code, position, cancellationToken);

    /// <inheritdoc />
    public Task<SignatureHelpInfo?> GetSignatureHelpAsync(string code, int position, CancellationToken cancellationToken = default)
        => _signature.GetSignatureHelpAsync(code, position, cancellationToken);
}
