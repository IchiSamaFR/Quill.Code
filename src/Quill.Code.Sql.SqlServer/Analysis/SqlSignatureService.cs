using Quill.Code.Analysis;

namespace Quill.Code.Sql.SqlServer.Analysis;

/// <summary>Default T-SQL signature help: nothing. Function and procedure signatures come from the database.</summary>
internal sealed class SqlSignatureService : ICodeSignature
{
    /// <inheritdoc />
    public Task<SignatureHelpInfo?> GetSignatureHelpAsync(string code, int position, CancellationToken cancellationToken = default)
        => Task.FromResult<SignatureHelpInfo?>(null);
}
