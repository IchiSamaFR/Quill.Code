using Quill.Code.Analysis;

namespace Quill.Code.Sql.Analysis;

/// <summary>Diagnostics T-SQL : fautes de syntaxe relevées par <see cref="SqlSyntaxChecker"/>.</summary>
public sealed class SqlDiagnosticService : ICodeDiagnostic
{
    /// <inheritdoc />
    public Task<IReadOnlyList<CodeDiagnostic>> GetDiagnosticsAsync(string code, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(code);

        return Task.FromResult(SqlSyntaxChecker.Check(code));
    }
}
