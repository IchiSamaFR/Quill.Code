using Quill.Code.Execution;
using Quill.Code.SqlServer.Execution;

namespace Quill.Code.SqlServer;

/// <summary>Execution de SQL sur un moteur, a partir d'une chaine de connexion. Une implementation par moteur.</summary>
public interface ISqlQueryExecutor
{
    /// <summary>
    /// Execute le script (lots GO compris) et collecte resultats et messages. Les erreurs SQL ne levent pas
    /// d'exception : elles sont rapportees dans <see cref="QueryExecutionResult.Messages"/>.
    /// </summary>
    /// <param name="options">
    /// Remise au fil de l'eau : les PRINT et messages du serveur par <see cref="RunOptions.Log"/>, chaque jeu de
    /// resultats par <see cref="DataRunOptions.Data"/> des qu'il est lu. Le resultat les contient aussi.
    /// </param>
    Task<QueryExecutionResult> ExecuteAsync(string connectionString, string script, DataRunOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Premier jeu de resultats d'une requete parametree (au plus <paramref name="maxRows"/> lignes) ;
    /// <paramref name="database"/> null = base de la chaine de connexion. Une erreur leve une exception.
    /// </summary>
    Task<QueryResultSet> ReadAsync(string connectionString, string? database, string query, IReadOnlyList<QueryParameter> parameters, int maxRows, CancellationToken cancellationToken = default);
}
