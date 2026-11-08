using Npgsql;
using System.Data.Common;
using Quill.Code.Constants;
using Quill.Code.Execution;
using Quill.Code.Scripting;
using Quill.Code.Sql.Execution;

namespace Quill.Code.Sql.PostgreSql;

/// <summary>
/// PostgreSQL execution (Npgsql): the script is sent as one command, Npgsql splits it into statements; an error
/// stops the following ones (and rolls back those of an implicit transaction). RAISE NOTICE is reported as Information.
/// </summary>
public sealed class PostgreSqlQueryExecutor : DbQueryExecutor
{
    private readonly string? _applicationName;

    /// <summary>PostgreSQL executor; <paramref name="applicationName"/> is seen by the server (pg_stat_activity).</summary>
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

        var lines = new List<string>(4) { $"{postgres.Severity} {postgres.SqlState}: {postgres.MessageText}" };
        if (!string.IsNullOrWhiteSpace(postgres.Detail))
            lines.Add($"Detail: {postgres.Detail}");
        if (!string.IsNullOrWhiteSpace(postgres.Hint))
            lines.Add($"Hint: {postgres.Hint}");
        return string.Join(Environment.NewLine, lines);
    }
}
