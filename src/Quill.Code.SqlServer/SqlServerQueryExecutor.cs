using Microsoft.Data.SqlClient;
using System.Data;
using System.Globalization;
using Quill.Code.Constants;
using Quill.Code.Execution;
using Quill.Code.Scripting;
using Quill.Code.SqlServer.Execution;
using Quill.Code.SqlServer.Constants;
using SqlBatch = Quill.Code.SqlServer.Execution.SqlBatch;

namespace Quill.Code.SqlServer;

/// <summary>
/// Execution SQL Server, calquee sur le comportement de SSMS : decoupage sur GO, un lot en echec n'empeche pas
/// les suivants. Tout est remis au fil de l'eau : PRINT et erreurs SQL (provenance Code), lignes affectees (Host) par Log, jeux de resultats
/// par Data. Une connexion est ouverte pour chaque appel.
/// </summary>
public sealed class SqlServerQueryExecutor : ISqlQueryExecutor
{
    private readonly string? _applicationName;

    /// <summary>Executeur SQL Server ; <paramref name="applicationName"/> est vu du serveur (sys.dm_exec_sessions).</summary>
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
                        // Comme SSMS : le lot suivant est tout de meme execute.
                        LogErrors(log, ex, batch.StartLine, LogSource.Code);
                    }
                }
            }
        }
        catch (SqlException ex) when (cancellationToken.IsCancellationRequested)
        {
            // L'annulation d'une commande en cours remonte sous forme de SqlException.
            throw new OperationCanceledException(ex.Message, ex, cancellationToken);
        }
        catch (SqlException ex)
        {
            // Hors d'un lot : connexion impossible, droits...
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

        var builder = new SqlConnectionStringBuilder(connectionString) { ConnectTimeout = SqlServerConstants.ReadConnectTimeoutSeconds };
        if (!string.IsNullOrWhiteSpace(_applicationName))
            builder.ApplicationName = _applicationName;
        if (!string.IsNullOrWhiteSpace(database))
            builder.InitialCatalog = database;

        await using var connection = new SqlConnection(builder.ConnectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var command = connection.CreateCommand();
#pragma warning disable CA2100 // La requete est celle de l'utilisateur ; les valeurs passent en parametres.
        command.CommandText = query;
#pragma warning restore CA2100
        command.CommandTimeout = SqlServerConstants.ReadCommandTimeoutSeconds;
        foreach (var parameter in parameters)
            command.Parameters.Add(CreateParameter(parameter));

        SqlDataReader reader;
        try
        {
            reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (SqlException ex) when (ex.Number == SqlServerConstants.UndeclaredVariableError)
        {
            // Variable inconnue : on liste ce qui a reellement ete envoye pour qu'un nom mal saisi se voie.
            var sent = parameters.Count == 0
                ? "aucun"
                : string.Join(", ", command.Parameters.Cast<SqlParameter>().Select(p => p.ParameterName));
            throw new InvalidOperationException($"{ex.Message} Paramètres envoyés : {sent}.", ex);
        }

        await using var _ = reader;

        // Premier jeu qui a des colonnes : un DECLARE ou un SET avant le SELECT n'en produit pas.
        while (reader.FieldCount == 0 && await reader.NextResultAsync(cancellationToken).ConfigureAwait(false))
        {
        }

        if (reader.FieldCount == 0)
            return new QueryResultSet(1, [], [], isTruncated: false);

        return await ReadResultSetAsync(reader, 1, maxRows, FormatForRead, cancellationToken).ConfigureAwait(false);
    }

    /// <returns>Nombre de jeux de resultats lus depuis le debut du script.</returns>
    private static async Task<int> ExecuteBatchAsync(SqlConnection connection, SqlBatch batch, int resultSetCount, DataRunOptions? options, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
#pragma warning disable CA2100 // Le script est celui de l'utilisateur, execute tel quel comme dans SSMS.
        command.CommandText = batch.Text;
#pragma warning restore CA2100
        // Pas de timeout : l'utilisateur garde la main via l'annulation.
        command.CommandTimeout = 0;
        command.StatementCompleted += (_, e) => options?.Log?.Invoke(new LogEntry(LogLevel.Information, string.Create(CultureInfo.InvariantCulture, $"({e.RecordCount} ligne(s) affectée(s))")));

        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        do
        {
            // FieldCount == 0 : instruction sans jeu de resultats (INSERT, UPDATE, DDL...).
            if (reader.FieldCount > 0)
            {
                var resultSet = await ReadResultSetAsync(reader, ++resultSetCount, QueryExecutionLimits.MaxRowsPerResultSet, FormatForResults, cancellationToken).ConfigureAwait(false);
                options?.Data?.Invoke(resultSet);
            }
        }
        while (await reader.NextResultAsync(cancellationToken).ConfigureAwait(false));

        return resultSetCount;
    }

    private static async Task<QueryResultSet> ReadResultSetAsync(SqlDataReader reader, int index, int maxRows, Func<object, string?> format, CancellationToken cancellationToken)
    {
        var fieldCount = reader.FieldCount;
        var columns = new QueryResultColumn[fieldCount];
        for (var i = 0; i < fieldCount; i++)
        {
            var name = reader.GetName(i);
            columns[i] = new QueryResultColumn(string.IsNullOrEmpty(name) ? QueryResultConstants.UnnamedColumn : name, reader.GetDataTypeName(i));
        }

        var rows = new List<QueryResultRow>();
        var unsupportedColumns = new bool[fieldCount];
        var isTruncated = false;

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            if (rows.Count == maxRows)
            {
                isTruncated = true;
                break;
            }

            var cells = new string?[fieldCount];
            for (var i = 0; i < fieldCount; i++)
                cells[i] = ReadCell(reader, i, unsupportedColumns, format);

            rows.Add(new QueryResultRow(rows.Count + 1, cells));
        }

        return new QueryResultSet(index, columns, rows, isTruncated);
    }

    private static string? ReadCell(SqlDataReader reader, int ordinal, bool[] unsupportedColumns, Func<object, string?> format)
    {
        if (!unsupportedColumns[ordinal])
        {
            try
            {
                var value = reader.GetValue(ordinal);
                return value is DBNull ? null : format(value);
            }
            catch (Exception ex) when (ex is InvalidCastException or FileNotFoundException or NotSupportedException)
            {
                // Types CLR (geography, hierarchyid...) dont l'assembly n'est pas disponible.
                unsupportedColumns[ordinal] = true;
            }
        }

        return $"<{reader.GetDataTypeName(ordinal)}>";
    }

    /// <summary>Grille de resultats : valeurs completes, comme SSMS.</summary>
    private static string? FormatForResults(object value) => value switch
    {
        string s => s,
        bool b => b ? "1" : "0",
        DateTime d => d.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture),
        DateTimeOffset d => d.ToString("yyyy-MM-dd HH:mm:ss.fffffff zzz", CultureInfo.InvariantCulture),
        byte[] bytes => FormatBinary(bytes, QueryResultConstants.ResultsMaxBinaryBytes),
        IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
        _ => value.ToString(),
    };

    /// <summary>Requete parametree (cartes, graphiques) : dates courtes, binaires tronques plus tot.</summary>
    private static string? FormatForRead(object value) => value switch
    {
        DateTime d => d.TimeOfDay == TimeSpan.Zero
            ? d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
            : d.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
        DateTimeOffset d => d.ToString("yyyy-MM-dd HH:mm:ss zzz", CultureInfo.InvariantCulture),
        byte[] bytes => FormatBinary(bytes, QueryResultConstants.ReadMaxBinaryBytes),
        _ => FormatForResults(value),
    };

    private static string FormatBinary(byte[] bytes, int maxBytes)
    {
        var shown = Convert.ToHexString(bytes, 0, Math.Min(bytes.Length, maxBytes));
        return bytes.Length > maxBytes ? $"0x{shown}…" : $"0x{shown}";
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

        // Un nom saisi a la main peut garder son @ ou des espaces.
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
            // Le serveur numerote les lignes a partir du debut du lot : on recale sur le script.
            var header = batchStartLine is { } start && error.LineNumber > 0
                ? string.Create(CultureInfo.InvariantCulture, $"Msg {error.Number}, Niveau {error.Class}, État {error.State}, Ligne {start + error.LineNumber - 1}")
                : string.Create(CultureInfo.InvariantCulture, $"Msg {error.Number}, Niveau {error.Class}, État {error.State}");

            log(new LogEntry(LogLevel.Error, $"{header}{Environment.NewLine}{error.Message}", source));
        }
    }
}
