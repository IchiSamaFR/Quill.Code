using Quill.Code.Analysis;
using Quill.Code.Completion;
using Quill.Code.Constants;
using Quill.Code.Sql.SqlServer.Analysis;
using Quill.Code.Sql.SqlServer.Completion;

namespace Quill.Code.Sql.SqlServer;

/// <summary>
/// T-SQL editor services: syntax errors (ScriptDom), completion and hover. Without a catalog, suggestions
/// are limited to keywords, document words and variables; with <see cref="SqlLanguageOptions.Catalog"/>,
/// databases, schemas, tables, views, procedures and columns are added.
/// </summary>
public sealed class SqlLanguageService : ICodeLanguageService
{
    private readonly SqlCompletionService _completion;
    private readonly SqlDiagnosticService _diagnostic;
    private readonly SqlHoverService _hover;
    private readonly SqlSignatureService _signature = new();

    /// <param name="options">Known catalog, database and variables; none by default.</param>
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
