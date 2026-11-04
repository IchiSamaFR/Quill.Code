using MySqlConnector;
using System.Data.Common;
using System.Globalization;
using Quill.Code.Constants;
using Quill.Code.Execution;
using Quill.Code.Scripting;

namespace Quill.Code.MySql;

/// <summary>
/// Execution MySQL / MariaDB (MySqlConnector) : le script part en une commande, le pilote enchaine ses instructions ;
/// une erreur arrete donc les instructions suivantes. <c>DELIMITER</c> (commande du client mysql) n'est pas reconnu.
/// </summary>
public sealed class MySqlQueryExecutor : DbQueryExecutor
{
    private readonly string? _applicationName;

    /// <summary>Executeur MySQL ; <paramref name="applicationName"/> est vu du serveur (attribut de connexion program_name).</summary>
    public MySqlQueryExecutor(string? applicationName = null)
    {
        _applicationName = applicationName;
    }

    /// <inheritdoc/>
    protected override DbConnection CreateConnection(string connectionString, string? database, bool forRead)
    {
        var builder = new MySqlConnectionStringBuilder(connectionString)
        {
            // Variables de session (@total) des scripts : refusees par defaut comme parametres non fournis.
            AllowUserVariables = true,
        };
        if (!string.IsNullOrWhiteSpace(_applicationName))
            builder.ApplicationName = _applicationName;
        if (database is not null)
            builder.Database = database;
        if (forRead)
            builder.ConnectionTimeout = QueryExecutionLimits.ReadConnectTimeoutSeconds;

        return new MySqlConnection(builder.ConnectionString);
    }

    /// <inheritdoc/>
    protected override void AttachMessages(DbConnection connection, Action<LogEntry> log)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(log);

        ((MySqlConnection)connection).InfoMessage += (_, e) =>
        {
            foreach (var error in e.Errors)
                log(new LogEntry(LogLevel.Information, error.Message, LogSource.Code));
        };
    }

    /// <inheritdoc/>
    protected override string FormatError(DbException exception)
        => exception is MySqlException mySql
            ? string.Create(CultureInfo.InvariantCulture, $"Erreur {mySql.Number} ({mySql.SqlState}){Environment.NewLine}{mySql.Message}")
            : base.FormatError(exception);
}
