using Oracle.ManagedDataAccess.Client;
using System.Data;
using System.Data.Common;
using Quill.Code.Constants;
using Quill.Code.Execution;
using Quill.Code.Sql.Oracle.Execution;
using Quill.Code.Scripting;
using Quill.Code.Sql.Execution;

namespace Quill.Code.Sql.Oracle;

/// <summary>
/// Oracle execution (ODP.NET Core): Oracle only accepts one statement per command, so the script is split as
/// in SQL*Plus (";" or a "/" line after a PL/SQL block) and a failed statement does not prevent the following ones.
/// Parameters bound by name (<c>:name</c>). An Oracle connection holds a single database: the requested database is ignored.
/// </summary>
public sealed class OracleQueryExecutor : DbQueryExecutor
{
    /// <inheritdoc/>
    protected override DbConnection CreateConnection(string connectionString, string? database, bool forRead)
    {
        var builder = new OracleConnectionStringBuilder(connectionString);
        if (forRead)
            builder.ConnectionTimeout = QueryExecutionLimits.ReadConnectTimeoutSeconds;

        return new OracleConnection(builder.ConnectionString);
    }

    /// <inheritdoc/>
    protected override IReadOnlyList<QueryBatch> SplitScript(string script) => OracleScriptSplitter.Split(script);

    /// <inheritdoc/>
    protected override void AttachMessages(DbConnection connection, Action<LogEntry> log)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(log);

        ((OracleConnection)connection).InfoMessage += (_, e) => log(new LogEntry(LogLevel.Information, e.Message, LogSource.Code));
    }

    /// <inheritdoc/>
    protected override void PrepareCommand(DbCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        // Positional binding by default: a repeated or out-of-order parameter would be bound wrongly.
        ((OracleCommand)command).BindByName = true;
    }

    /// <inheritdoc/>
    protected override string GetParameterName(string name) => name;

    /// <inheritdoc/>
    protected override DbType GetDbType(QueryParameterType type) => type switch
    {
        // No SQL boolean before 23ai nor Guid type: NUMBER(1) and RAW(16), like EF Core Oracle.
        QueryParameterType.Boolean => DbType.Int16,
        QueryParameterType.Guid => DbType.Binary,
        _ => base.GetDbType(type),
    };

    /// <inheritdoc/>
    protected override object GetParameterValue(QueryParameter parameter)
    {
        ArgumentNullException.ThrowIfNull(parameter);

        return parameter.Value switch
        {
            bool b => (short)(b ? 1 : 0),
            Guid g => g.ToByteArray(),
            _ => base.GetParameterValue(parameter),
        };
    }
}
