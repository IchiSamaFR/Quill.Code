using Quill.Code.Analysis;

namespace Quill.Code.Sql.SqlServer.Analysis;

/// <summary>Diagnostics T-SQL : fautes de syntaxe relevées par <see cref="SqlSyntaxChecker"/>.</summary>
internal sealed class SqlDiagnosticService : ICodeDiagnostic
{
    private readonly SqlLanguageOptions _options;

    public SqlDiagnosticService(SqlLanguageOptions options)
    {
        _options = options;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<CodeDiagnostic>> GetDiagnosticsAsync(string code, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(code);

        return Task.FromResult<IReadOnlyList<CodeDiagnostic>>(_options.CheckSyntax ? SqlSyntaxChecker.Check(code) : []);
    }
}
