using Quill.Data.Sql;

namespace Quill.Code.SqlServer.Schema;

/// <summary>
/// Catalogue des bases, fourni par l'hôte : Quill ne se connecte à rien. Une base indisponible (connexion fermée,
/// droits insuffisants…) se rend vide ou null plutôt que par une exception ; Quill traite de toute façon une
/// exception autre qu'une annulation comme une absence de schéma.
/// </summary>
public interface ISqlCatalog
{
    /// <summary>Bases du serveur (USE, base.schéma.objet).</summary>
    Task<IReadOnlyList<SqlDatabaseInfo>> GetDatabasesAsync(CancellationToken cancellationToken = default);

    /// <summary>Schéma complet d'une base, colonnes comprises ; null si elle est inconnue ou indisponible.</summary>
    /// <param name="database">Nom de la base ; null = base par défaut de la connexion.</param>
    Task<SqlDatabaseSchema?> GetDatabaseAsync(string? database, CancellationToken cancellationToken = default);
}
