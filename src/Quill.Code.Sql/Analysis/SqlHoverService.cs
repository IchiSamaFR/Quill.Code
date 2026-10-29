using Quill.Code.Analysis;

namespace Quill.Code.Sql.Analysis;

/// <summary>Survol T-SQL par défaut : rien. Décrire une table ou une colonne demande le schéma de la base.</summary>
public sealed class SqlHoverService : ICodeHover
{
    /// <inheritdoc />
    public Task<HoverInfo?> GetHoverAsync(string code, int position, CancellationToken cancellationToken = default)
        => Task.FromResult<HoverInfo?>(null);
}
