using Npgsql;
using System.Data.Common;
using Quill.Code.Constants;
using Quill.Code.Execution;
using Quill.Code.Scripting;

namespace Quill.Code.PostgreSql;

/// <summary>
/// Execution PostgreSQL (Npgsql) : le script part en une commande, Npgsql le decoupe en instructions ; une erreur
/// arrete les suivantes (et annule celles d'une transaction implicite). RAISE NOTICE remonte en Information.
/// </summary>
public sealed class PostgreSqlQueryExecutor : DbQueryExecutor
{
    private readonly string? _applicationName;

    /// <summary>Executeur PostgreSQL ; <paramref name="applicationName"/> est vu du serveur (pg_stat_activity).</summary>
    public PostgreSqlQueryExecutor(string? applicationName = null)
    {
        _applicationName = applicationName;
    }

    /// <inheritdoc/>
    protected override DbConnection CreateConnection(string connectionString, string? database, bool forRead)
    {
        var builder = new NpgsqlConnectionStringBuilder(connectionString);
        if (!string.IsNullOrWhiteSpace(_applicationName))
            builder.ApplicationName = _applicationName;
        if (database is not null)
            builder.Database = database;
        if (forRead)
            builder.Timeout = QueryExecutionLimits.ReadConnectTimeoutSeconds;

        return new NpgsqlConnection(builder.ConnectionString);
    }

    /// <inheritdoc/>
    protected override void AttachMessages(DbConnection connection, Action<LogEntry> log)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(log);

        ((NpgsqlConnection)connection).Notice += (_, e) => log(new LogEntry(LogLevel.Information, e.Notice.MessageText, LogSource.Code));
    }

    /// <inheritdoc/>
    protected override string FormatError(DbException exception)
    {
        if (exception is not PostgresException postgres)
            return base.FormatError(exception);

        var lines = new List<string>(4) { $"{postgres.Severity} {postgres.SqlState} : {postgres.MessageText}" };
        if (!string.IsNullOrWhiteSpace(postgres.Detail))
            lines.Add($"Détail : {postgres.Detail}");
        if (!string.IsNullOrWhiteSpace(postgres.Hint))
            lines.Add($"Conseil : {postgres.Hint}");
        return string.Join(Environment.NewLine, lines);
    }
}
