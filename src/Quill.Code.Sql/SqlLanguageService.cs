using Quill.Code.Analysis;
using Quill.Code.Completion;
using Quill.Code.Languages;
using Quill.Code.Sql.Analysis;
using Quill.Code.Sql.Completion;

namespace Quill.Code.Sql;

/// <summary>
/// Point d'entrée T-SQL : relaie chaque demande au service de la fonctionnalité (complétion, diagnostics, survol,
/// signature). Par défaut ce sont ceux de Quill (diagnostics de syntaxe seulement) ; l'hôte qui connaît le schéma de la
/// base fournit les siens pour les autres.
/// </summary>
public sealed class SqlLanguageService : ICodeLanguageProvider
{
    private readonly ICodeCompletion _completion;
    private readonly ICodeDiagnostic _diagnostic;
    private readonly ICodeHover _hover;
    private readonly ICodeSignature _signature;

    /// <summary>Services choisis par l'hôte ; chaque <c>null</c> garde celui de Quill.</summary>
    public SqlLanguageService(ICodeCompletion? completion = null, ICodeDiagnostic? diagnostic = null, ICodeHover? hover = null, ICodeSignature? signature = null)
    {
        _completion = completion ?? new SqlCompletionService();
        _diagnostic = diagnostic ?? new SqlDiagnosticService();
        _hover = hover ?? new SqlHoverService();
        _signature = signature ?? new SqlSignatureService();
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
