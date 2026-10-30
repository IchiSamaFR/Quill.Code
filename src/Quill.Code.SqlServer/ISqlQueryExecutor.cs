using Quill.Code.Execution;
using Quill.Code.SqlServer.Execution;

namespace Quill.Code.SqlServer;

/// <summary>Execution de SQL sur un moteur, a partir d'une chaine de connexion. Une implementation par moteur.</summary>
public interface ISqlQueryExecutor
{
    /// <summary>
    /// Execute le script (lots GO compris), sans rien rendre : tout est remis au fil de l'eau par
    /// <paramref name="options"/>. Comme SSMS, une erreur SQL n'arrete pas les lots suivants et ne leve pas
    /// d'exception : elle est remise par <see cref="RunOptions.Log"/> au niveau Error.
    /// </summary>
    /// <param name="options">
    /// <see cref="RunOptions.Log"/> : PRINT et messages du serveur (Information, Code), lignes affectees (Information, Host), erreurs (Error, Code d'un lot, Host sinon) ;
    /// <see cref="DataRunOptions.Data"/> : chaque jeu de resultats des qu'il est lu.
    /// </param>
    /// <exception cref="OperationCanceledException">Execution annulee.</exception>
    Task RunAsync(string connectionString, string script, DataRunOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Premier jeu de resultats d'une requete parametree (au plus <paramref name="maxRows"/> lignes) ;
    /// <paramref name="database"/> null = base de la chaine de connexion. Une erreur leve une exception.
    /// </summary>
    Task<QueryResultSet> ReadAsync(string connectionString, string? database, string query, IReadOnlyList<QueryParameter> parameters, int maxRows, CancellationToken cancellationToken = default);
}
