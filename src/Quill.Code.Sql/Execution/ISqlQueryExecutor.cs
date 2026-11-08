using Quill.Code.Execution;

namespace Quill.Code.Sql.Execution;

/// <summary>Executes SQL on an engine, from a connection string. One implementation per engine.</summary>
public interface ISqlQueryExecutor
{
    /// <summary>
    /// Runs the script without returning anything: everything is handed over as it comes through <paramref name="options"/>.
    /// A failed batch (GO batch on SQL Server, statement on Oracle) does not stop the following ones and does not throw:
    /// the error is reported through <see cref="RunOptions.Log"/> at Error level.
    /// </summary>
    /// <param name="options">
    /// <see cref="RunOptions.Log"/>: server messages (Information, Code), affected rows (Information, Host), errors (Error, Code for a batch, Host otherwise);
    /// <see cref="DataRunOptions.Data"/>: each result set as soon as it is read.
    /// </param>
    /// <exception cref="OperationCanceledException">Execution canceled.</exception>
    Task RunAsync(string connectionString, string script, DataRunOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// First result set of a parameterized query (at most <paramref name="maxRows"/> rows);
    /// <paramref name="database"/> null = database of the connection string. An error throws an exception.
    /// </summary>
    Task<QueryResultSet> ReadAsync(string connectionString, string? database, string query, IReadOnlyList<QueryParameter> parameters, int maxRows, CancellationToken cancellationToken = default);
}
