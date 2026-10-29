using Quill.Code.Analysis;

namespace Quill.Code.Sql.Analysis;

/// <summary>Aide à la signature T-SQL par défaut : rien. Les signatures des fonctions et procédures viennent de la base.</summary>
internal sealed class SqlSignatureService : ICodeSignature
{
    /// <inheritdoc />
    public Task<SignatureHelpInfo?> GetSignatureHelpAsync(string code, int position, CancellationToken cancellationToken = default)
        => Task.FromResult<SignatureHelpInfo?>(null);
}
