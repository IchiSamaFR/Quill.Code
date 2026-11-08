using System.Data;
using System.Data.Common;
using System.Globalization;
using Quill.Code.Constants;
using Quill.Code.Scripting;
using Quill.Code.Execution;

namespace Quill.Code.Sql.Execution;

/// <summary>
/// SQL execution on an ADO.NET driver, base of the engine executors (MySQL, PostgreSQL, Oracle, SQLite): same contract
/// as <c>SqlServerQueryExecutor</c>. The script is split into batches by <see cref="SplitScript"/> (a single one by default:
/// the driver chains the statements); a failed batch does not prevent the following ones. A connection is opened for each call.
/// </summary>
public abstract class DbQueryExecutor : ISqlQueryExecutor
{
    /// <inheritdoc/>
    public async Task RunAsync(string connectionString, string script, DataRunOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(script);

        var log = options?.Log;
        var batches = SplitScript(script);
        if (batches.Count == 0)
        {
            log?.Invoke(new LogEntry(LogLevel.Information, "Aucune instruction à exécuter."));
            return;
        }

        try
        {
            await using var connection = CreateConnection(connectionString, database: null, forRead: false);
            if (log is not null)
                AttachMessages(connection, log);

            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            var resultSetCount = 0;
            foreach (var batch in batches)
            {
                try
                {
                    resultSetCount = await ExecuteBatchAsync(connection, batch, resultSetCount, options, cancellationToken).ConfigureAwait(false);
                }
                catch (DbException ex) when (!cancellationToken.IsCancellationRequested)
                {
                    // Le lot suivant est tout de meme execute ; la ligne n'a de sens que si le script a ete decoupe.
                    LogError(log, ex, batches.Count > 1 ? batch.StartLine : null, LogSource.Code);
                }
            }
        }
        catch (DbException ex) when (cancellationToken.IsCancellationRequested)
        {
            // L'annulation d'une commande en cours remonte souvent sous forme d'exception du pilote.
            throw new OperationCanceledException(ex.Message, ex, cancellationToken);
        }
        catch (DbException ex)
        {
            // Hors d'un lot : connexion impossible, droits...
            LogError(log, ex, batchStartLine: null, LogSource.Host);
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

        await using var connection = CreateConnection(connectionString, string.IsNullOrWhiteSpace(database) ? null : database, forRead: true);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var command = connection.CreateCommand();
#pragma warning disable CA2100 // La requete est celle de l'utilisateur ; les valeurs passent en parametres.
        command.CommandText = query;
#pragma warning restore CA2100
        command.CommandTimeout = QueryExecutionLimits.ReadCommandTimeoutSeconds;
        PrepareCommand(command);
        foreach (var parameter in parameters)
            command.Parameters.Add(CreateParameter(command, parameter));

        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

        // Premier jeu qui a des colonnes : une instruction sans resultat peut preceder le SELECT.
        while (reader.FieldCount == 0 && await reader.NextResultAsync(cancellationToken).ConfigureAwait(false))
        {
        }

        if (reader.FieldCount == 0)
            return new QueryResultSet(1, [], [], isTruncated: false);

        return await DbResultSetReader.ReadAsync(reader, 1, maxRows, DbResultSetReader.FormatForRead, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Unopened connection on <paramref name="database"/> (null: the one in the string); <paramref name="forRead"/>:
    /// parameterized query, to open within <see cref="QueryExecutionLimits.ReadConnectTimeoutSeconds"/>.
    /// Throws <see cref="ArgumentException"/> or <see cref="FormatException"/> if the string is invalid.
    /// </summary>
    protected abstract DbConnection CreateConnection(string connectionString, string? database, bool forRead);

    /// <summary>Batches of the script, in order; by default the whole script, the driver chaining its statements.</summary>
    protected virtual IReadOnlyList<QueryBatch> SplitScript(string script)
        => string.IsNullOrWhiteSpace(script) ? [] : [new QueryBatch(script, 1)];

    /// <summary>Wires server messages (NOTICE, warnings...) to <paramref name="log"/>, before opening.</summary>
    protected virtual void AttachMessages(DbConnection connection, Action<LogEntry> log)
    {
    }

    /// <summary>Driver-specific settings, applied to each command (e.g. binding parameters by name).</summary>
    protected virtual void PrepareCommand(DbCommand command)
    {
    }

    /// <summary>Parameter name as the driver expects it, from the name entered without prefix.</summary>
    protected virtual string GetParameterName(string name) => $"@{name}";

    /// <summary>ADO.NET type of a parameter.</summary>
    protected virtual DbType GetDbType(QueryParameterType type) => type switch
    {
        QueryParameterType.Integer => DbType.Int64,
        QueryParameterType.Decimal => DbType.Decimal,
        QueryParameterType.Date => DbType.Date,
        QueryParameterType.DateTime => DbType.DateTime,
        QueryParameterType.Boolean => DbType.Boolean,
        QueryParameterType.Guid => DbType.Guid,
        _ => DbType.String,
    };

    /// <summary>Value sent to the driver; null becomes <see cref="DBNull"/>.</summary>
    protected virtual object GetParameterValue(QueryParameter parameter)
    {
        ArgumentNullException.ThrowIfNull(parameter);
        return parameter.Value ?? DBNull.Value;
    }

    /// <summary>Text of a server error; by default the driver's message.</summary>
    protected virtual string FormatError(DbException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return exception.Message;
    }

    /// <returns>Number of result sets read since the start of the script.</returns>
    private async Task<int> ExecuteBatchAsync(DbConnection connection, QueryBatch batch, int resultSetCount, DataRunOptions? options, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
#pragma warning disable CA2100 // Le script est celui de l'utilisateur, execute tel quel.
        command.CommandText = batch.Text;
#pragma warning restore CA2100
        // Pas de timeout : l'utilisateur garde la main via l'annulation.
        command.CommandTimeout = 0;
        PrepareCommand(command);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        do
        {
            // FieldCount == 0 : instruction sans jeu de resultats (INSERT, UPDATE, DDL...).
            if (reader.FieldCount > 0)
            {
                var resultSet = await DbResultSetReader.ReadAsync(reader, ++resultSetCount, QueryExecutionLimits.MaxRowsPerResultSet, DbResultSetReader.FormatForResults, cancellationToken).ConfigureAwait(false);
                options?.Data?.Invoke(resultSet);
            }
        }
        while (await reader.NextResultAsync(cancellationToken).ConfigureAwait(false));

        // Cumul du lot, connu une fois tous les jeux parcourus ; -1 pour un SELECT seul.
        if (reader.RecordsAffected >= 0)
            options?.Log?.Invoke(new LogEntry(LogLevel.Information, string.Create(CultureInfo.InvariantCulture, $"({reader.RecordsAffected} ligne(s) affectée(s))")));

        return resultSetCount;
    }

    private DbParameter CreateParameter(DbCommand command, QueryParameter parameter)
    {
        var result = command.CreateParameter();

        // Un nom saisi a la main peut garder son prefixe (@, :) ou des espaces.
        result.ParameterName = GetParameterName(parameter.Name.Trim().TrimStart('@', ':'));
        result.DbType = GetDbType(parameter.Type);
        result.Value = GetParameterValue(parameter);
        return result;
    }

    private void LogError(Action<LogEntry>? log, DbException exception, int? batchStartLine, LogSource source)
    {
        if (log is null)
            return;

        var message = FormatError(exception);
        var text = batchStartLine is { } line
            ? string.Create(CultureInfo.InvariantCulture, $"Instruction ligne {line}{Environment.NewLine}{message}")
            : message;

        log(new LogEntry(LogLevel.Error, text, source));
    }
}
