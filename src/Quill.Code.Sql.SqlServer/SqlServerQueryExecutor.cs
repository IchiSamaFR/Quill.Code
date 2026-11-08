using Microsoft.Data.SqlClient;
using System.Data;
using System.Globalization;
using Quill.Code.Constants;
using Quill.Code.Execution;
using Quill.Code.Scripting;
using Quill.Code.Sql.SqlServer.Execution;
using Quill.Code.Sql.SqlServer.Constants;
using SqlBatch = Quill.Code.Sql.SqlServer.Execution.SqlBatch;
using Quill.Code.Sql.Execution;

namespace Quill.Code.Sql.SqlServer;

/// <summary>
/// SQL Server execution, modeled on SSMS behavior: split on GO, a failed batch does not prevent
/// the following ones. Everything is handed over as it comes: PRINT and SQL errors (Code origin), affected rows (Host) through Log, result sets
/// through Data. A connection is opened for each call.
/// </summary>
public sealed class SqlServerQueryExecutor : ISqlQueryExecutor
{
    private readonly string? _applicationName;

    /// <summary>SQL Server executor; <paramref name="applicationName"/> is seen by the server (sys.dm_exec_sessions).</summary>
    public SqlServerQueryExecutor(string? applicationName = null)
    {
        _applicationName = applicationName;
    }

    /// <inheritdoc/>
    public async Task RunAsync(string connectionString, string script, DataRunOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(script);

        var log = options?.Log;
        var batches = SqlBatchSplitter.Split(script);
        if (batches.Count == 0)
        {
            log?.Invoke(new LogEntry(LogLevel.Information, "Aucune instruction à exécuter."));
            return;
        }

        try
        {
            var builder = new SqlConnectionStringBuilder(connectionString);
            if (!string.IsNullOrWhiteSpace(_applicationName))
                builder.ApplicationName = _applicationName;

            await using var connection = new SqlConnection(builder.ConnectionString);
            connection.InfoMessage += (_, e) =>
            {
                foreach (SqlError error in e.Errors)
                    log?.Invoke(new LogEntry(LogLevel.Information, error.Message, LogSource.Code));
            };

            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            var resultSetCount = 0;
            foreach (var batch in batches)
            {
                for (var run = 0; run < batch.RepeatCount; run++)
                {
                    try
                    {
                        resultSetCount = await ExecuteBatchAsync(connection, batch, resultSetCount, options, cancellationToken).ConfigureAwait(false);
                    }
                    catch (SqlException ex) when (!cancellationToken.IsCancellationRequested)
                    {
                        // Like SSMS: the next batch still runs.
                        LogErrors(log, ex, batch.StartLine, LogSource.Code);
                    }
                }
            }
        }
        catch (SqlException ex) when (cancellationToken.IsCancellationRequested)
        {
            // Canceling a running command surfaces as a SqlException.
            throw new OperationCanceledException(ex.Message, ex, cancellationToken);
        }
        catch (SqlException ex)
        {
            // Outside a batch: connection impossible, permissions...
            LogErrors(log, ex, batchStartLine: null, LogSource.Host);
        }
        catch (Exception ex) when (ex is ArgumentException or FormatException or InvalidOperationException)
        {
            log?.Invoke(new LogEntry(LogLevel.Error, $"Échec : {ex.Message}"));
        }
    }

    /// <inheritdoc/>
    public async Task<QueryResultSet> ReadAsync(string connectionString, string? database, string query, IReadOnlyList<QueryParameter> parameters, int maxRows, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(query);
        ArgumentNullException.ThrowIfNull(parameters);

        var builder = new SqlConnectionStringBuilder(connectionString) { ConnectTimeout = QueryExecutionLimits.ReadConnectTimeoutSeconds };
        if (!string.IsNullOrWhiteSpace(_applicationName))
            builder.ApplicationName = _applicationName;
        if (!string.IsNullOrWhiteSpace(database))
            builder.InitialCatalog = database;

        await using var connection = new SqlConnection(builder.ConnectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var command = connection.CreateCommand();
#pragma warning disable CA2100 // The query is the user's; values are passed as parameters.
        command.CommandText = query;
#pragma warning restore CA2100
        command.CommandTimeout = QueryExecutionLimits.ReadCommandTimeoutSeconds;
        foreach (var parameter in parameters)
            command.Parameters.Add(CreateParameter(parameter));

        SqlDataReader reader;
        try
        {
            reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (SqlException ex) when (ex.Number == SqlServerConstants.UndeclaredVariableError)
        {
            // Unknown variable: list what was actually sent so that a mistyped name shows.
            var sent = parameters.Count == 0
                ? "aucun"
                : string.Join(", ", command.Parameters.Cast<SqlParameter>().Select(p => p.ParameterName));
            throw new InvalidOperationException($"{ex.Message} Paramètres envoyés : {sent}.", ex);
        }

        await using var _ = reader;

        // First set that has columns: a DECLARE or SET before the SELECT produces none.
        while (reader.FieldCount == 0 && await reader.NextResultAsync(cancellationToken).ConfigureAwait(false))
        {
        }

        if (reader.FieldCount == 0)
            return new QueryResultSet(1, [], [], isTruncated: false);

        return await DbResultSetReader.ReadAsync(reader, 1, maxRows, DbResultSetReader.FormatForRead, cancellationToken).ConfigureAwait(false);
    }

    /// <returns>Number of result sets read since the start of the script.</returns>
    private static async Task<int> ExecuteBatchAsync(SqlConnection connection, SqlBatch batch, int resultSetCount, DataRunOptions? options, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
#pragma warning disable CA2100 // The script is the user's, run as is like in SSMS.
        command.CommandText = batch.Text;
#pragma warning restore CA2100
        // No timeout: the user keeps control through cancellation.
        command.CommandTimeout = 0;
        command.StatementCompleted += (_, e) => options?.Log?.Invoke(new LogEntry(LogLevel.Information, string.Create(CultureInfo.InvariantCulture, $"({e.RecordCount} ligne(s) affectée(s))")));

        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        do
        {
            // FieldCount == 0: statement without a result set (INSERT, UPDATE, DDL...).
            if (reader.FieldCount > 0)
            {
                var resultSet = await DbResultSetReader.ReadAsync(reader, ++resultSetCount, QueryExecutionLimits.MaxRowsPerResultSet, DbResultSetReader.FormatForResults, cancellationToken).ConfigureAwait(false);
                options?.Data?.Invoke(resultSet);
            }
        }
        while (await reader.NextResultAsync(cancellationToken).ConfigureAwait(false));

        return resultSetCount;
    }

    private static SqlParameter CreateParameter(QueryParameter parameter)
    {
        var (type, size) = parameter.Type switch
        {
            QueryParameterType.Integer => (SqlDbType.BigInt, 0),
            QueryParameterType.Decimal => (SqlDbType.Decimal, 0),
            QueryParameterType.Date => (SqlDbType.Date, 0),
            QueryParameterType.DateTime => (SqlDbType.DateTime2, 0),
            QueryParameterType.Boolean => (SqlDbType.Bit, 0),
            QueryParameterType.Guid => (SqlDbType.UniqueIdentifier, 0),
            _ => (SqlDbType.NVarChar, -1),
        };

        // A name entered by hand may keep its @ or spaces.
        var result = new SqlParameter($"@{parameter.Name.Trim().TrimStart('@')}", type) { Value = parameter.Value ?? DBNull.Value };
        if (size != 0)
            result.Size = size;
        return result;
    }

    private static void LogErrors(Action<LogEntry>? log, SqlException exception, int? batchStartLine, LogSource source)
    {
        if (log is null)
            return;

        foreach (SqlError error in exception.Errors)
        {
            // The server numbers lines from the start of the batch: realign on the script.
            var header = batchStartLine is { } start && error.LineNumber > 0
                ? string.Create(CultureInfo.InvariantCulture, $"Msg {error.Number}, Niveau {error.Class}, État {error.State}, Ligne {start + error.LineNumber - 1}")
                : string.Create(CultureInfo.InvariantCulture, $"Msg {error.Number}, Niveau {error.Class}, État {error.State}");

            log(new LogEntry(LogLevel.Error, $"{header}{Environment.NewLine}{error.Message}", source));
        }
    }
}
