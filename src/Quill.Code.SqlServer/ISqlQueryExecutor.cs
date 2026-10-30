using Quill.Code.SqlServer.Execution;

namespace Quill.Code.SqlServer;

/// <summary>Execution de SQL sur un moteur, a partir d'une chaine de connexion. Une implementation par moteur.</summary>
public interface ISqlQueryExecutor
{
    /// <summary>
    /// Execute le script (lots GO compris) et collecte resultats et messages. Les erreurs SQL ne levent pas
    /// d'exception : elles sont rapportees dans <see cref="QueryExecutionResult.Messages"/>.
    /// </summary>
    Task<QueryExecutionResult> ExecuteAsync(string connectionString, string script, CancellationToken cancellationToken = default);

    /// <summary>
    /// Premier jeu de resultats d'une requete parametree (au plus <paramref name="maxRows"/> lignes) ;
    /// <paramref name="database"/> null = base de la chaine de connexion. Une erreur leve une exception.
    /// </summary>
    Task<QueryResultSet> ReadAsync(string connectionString, string? database, string query, IReadOnlyList<QueryParameter> parameters, int maxRows, CancellationToken cancellationToken = default);
}
