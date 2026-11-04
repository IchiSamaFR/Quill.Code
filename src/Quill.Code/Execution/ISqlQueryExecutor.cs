namespace Quill.Code.Execution;

/// <summary>Execution de SQL sur un moteur, a partir d'une chaine de connexion. Une implementation par moteur.</summary>
public interface ISqlQueryExecutor
{
    /// <summary>
    /// Execute le script, sans rien rendre : tout est remis au fil de l'eau par <paramref name="options"/>.
    /// Un lot en echec (lot GO sous SQL Server, instruction sous Oracle) n'arrete pas les suivants et ne leve pas
    /// d'exception : l'erreur est remise par <see cref="RunOptions.Log"/> au niveau Error.
    /// </summary>
    /// <param name="options">
    /// <see cref="RunOptions.Log"/> : messages du serveur (Information, Code), lignes affectees (Information, Host), erreurs (Error, Code d'un lot, Host sinon) ;
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
