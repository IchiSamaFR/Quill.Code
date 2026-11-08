using Quill.Code.Analysis;
using Quill.Code.Constants;
using Quill.Code.CSharp.Compilation;
using Quill.Code.CSharp.Linq.Compilation;
using Quill.Code.CSharp.Linq.Constants;
using Quill.Code.CSharp.Linq.Execution;
using Quill.Code.Execution;
using Quill.Code.Scripting;
using Quill.Data.Sql;
using System.Data.Common;
using System.Globalization;

namespace Quill.Code.CSharp.Linq;

/// <summary>
/// LINQ execution on the database of an <see cref="ILinqDatabaseProvider"/>, modeled on the SQL execution of <c>Quill.Code.Sql.SqlServer</c>: <see cref="RunAsync"/>
/// returns nothing and hands everything over as it comes (each <c>Dump(value, title)</c> through Data, generated SQL, <c>Log.*</c> and
/// errors through Log), <see cref="ReadAsync"/> returns a set and throws its errors. The query is compiled against the
/// DbContext generated from the database structure, in the very project of <see cref="LinqLanguageService"/>.
/// An EF context is created for each call.
/// </summary>
public sealed class LinqQueryExecutor
{
    private readonly ILinqDatabaseProvider _provider;

    /// <summary>LINQ executor on the engine of <paramref name="provider"/> (same instance as the editor: same compiled DbContext).</summary>
    public LinqQueryExecutor(ILinqDatabaseProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);
        _provider = provider;
    }

    /// <summary>
    /// Runs the query without returning anything: everything is handed over as it comes through <paramref name="options"/>. A
    /// compilation error or a script exception does not throw: it is reported through <see cref="RunOptions.Log"/>
    /// at Error level.
    /// </summary>
    /// <param name="schema">Structure of the database of <paramref name="connectionString"/>; the same instance gives the same compiled DbContext.</param>
    /// <param name="options">
    /// <see cref="RunOptions.Log"/>: context, generated SQL and rows read (Information, Host), the script's <c>Log.*</c> (Code),
    /// errors (Error, Code for the query, Host for the generated code);
    /// <see cref="DataRunOptions.Data"/>: each Dump, converted to a result set as soon as it is called.
    /// </param>
    /// <exception cref="OperationCanceledException">
    /// Execution canceled; <see cref="CSharpRunAbandonedException"/> when the script, which cannot be interrupted,
    /// finishes in the background (it keeps its EF context until then).
    /// </exception>
    public async Task RunAsync(string connectionString, SqlDatabaseSchema schema, string script, DataRunOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(schema);
        ArgumentNullException.ThrowIfNull(script);

        var log = options?.Log;
        try
        {
            connectionString = _provider.BuildConnectionString(connectionString, database: null);
        }
        catch (Exception ex) when (ex is ArgumentException or FormatException)
        {
            log?.Invoke(new LogEntry(LogLevel.Error, $"Échec : {ex.Message}"));
            return;
        }

        LinqParametersAssembly globals;
        try
        {
            var model = await LinqContexts.GetModelAsync(_provider, schema, cancellationToken).ConfigureAwait(false);
            log?.Invoke(new LogEntry(LogLevel.Information, string.Create(CultureInfo.InvariantCulture,
                $"Contexte {model.Model.ContextClassName} ({LinqQueryConstants.ContextAlias}) : {model.Model.EntityClassNames.Count} table(s) et vue(s).")));

            globals = await LinqContexts.GetParametersAsync(model, cancellationToken: cancellationToken).ConfigureAwait(false);
        }
        catch (LinqCompilationException ex)
        {
            LogGeneratedErrors(log, ex);
            return;
        }

        List<QueryResultSet> resultSets = [];
        var gate = new Lock();
        var output = new LogOutput(log);
        var runOptions = new DataRunOptions
        {
            Log = output.Write,
            Data = resultSet =>
            {
                lock (gate)
                    resultSets.Add(resultSet);

                options?.Data?.Invoke(resultSet);
            },
        };

        try
        {
            await RunScriptAsync(globals, script, connectionString, runOptions, QueryExecutionLimits.MaxRowsPerResultSet, values: null, cancellationToken).ConfigureAwait(false);
        }
        catch (CSharpCompilationException ex)
        {
            foreach (var error in ex.Errors)
                log?.Invoke(new LogEntry(LogLevel.Error, error.Describe(script), LogSource.Code));
            return;
        }
        catch (CSharpRunAbandonedException)
        {
            log?.Invoke(new LogEntry(LogLevel.Error, "Le code LINQ ne peut pas être interrompu : il se termine en arrière-plan et garde sa connexion ouverte jusque-là."));
            throw;
        }
        catch (LinqCompilationException ex)
        {
            LogGeneratedErrors(log, ex);
            return;
        }
        catch (DbException ex) when (cancellationToken.IsCancellationRequested)
        {
            // Like SQL Server: canceling a running command surfaces as a driver error.
            throw new OperationCanceledException(ex.Message, ex, cancellationToken);
        }
        catch (DbException ex)
        {
            log?.Invoke(new LogEntry(LogLevel.Error, ex.Message, LogSource.Code));
            return;
        }
#pragma warning disable CA1031 // The script is user code: any exception (EF translation, null, division...) is a result.
        catch (Exception ex) when (ex is not OperationCanceledException)
#pragma warning restore CA1031
        {
            log?.Invoke(new LogEntry(LogLevel.Error, $"{ex.GetType().Name} : {ex.Message}", LogSource.Code));
            return;
        }

        QueryResultSet[] dumped;
        lock (gate)
            dumped = [.. resultSets];

        foreach (var resultSet in dumped)
        {
            // Several sets: each message says which tab it relates to.
            var prefix = dumped.Length > 1 ? $"{resultSet.Title} : " : string.Empty;
            log?.Invoke(new LogEntry(LogLevel.Information, string.Create(CultureInfo.InvariantCulture, $"{prefix}({resultSet.Rows.Count} ligne(s))")));
        }

        if (dumped.Length == 0)
            log?.Invoke(new LogEntry(LogLevel.Information, "Aucun résultat : affichez les données avec .Dump()."));
    }

    /// <summary>
    /// Set of the last Dump of a parameterized query (at most <paramref name="maxRows"/> rows);
    /// <paramref name="database"/> null = database of the connection string. An error throws an exception.
    /// </summary>
    /// <param name="schema">Structure of the queried database; the same instance gives the same compiled DbContext.</param>
    /// <param name="parameters">Values usable by name (without @), sent by EF as SQL parameters.</param>
    /// <exception cref="InvalidOperationException">The query does not compile, a value does not fit its type, or nothing is displayed.</exception>
    public async Task<QueryResultSet> ReadAsync(string connectionString, string? database, SqlDatabaseSchema schema, string query, IReadOnlyList<QueryParameter> parameters, int maxRows, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(schema);
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(parameters);

        connectionString = _provider.BuildConnectionString(connectionString, database);
        var variables = parameters.Select(LinqParameter.From).ToArray();
        List<QueryResultSet> resultSets = [];
        try
        {
            var model = await LinqContexts.GetModelAsync(_provider, schema, cancellationToken).ConfigureAwait(false);
            var globals = await LinqContexts.GetParametersAsync(model, variables, cancellationToken).ConfigureAwait(false);
            await RunScriptAsync(globals, query, connectionString, new DataRunOptions { Data = resultSets.Add }, Math.Max(1, maxRows), variables, cancellationToken).ConfigureAwait(false);
        }
        catch (CSharpCompilationException ex)
        {
            throw new InvalidOperationException(string.Join(Environment.NewLine, ex.Errors.Take(LinqExecutionConstants.MaxReadErrors).Select(e => e.Describe(query))), ex);
        }
        catch (LinqCompilationException ex)
        {
            throw new InvalidOperationException(ex.Message, ex);
        }

        // A read only displays one set: the last Dump().
        return resultSets.Count > 0
            ? resultSets[^1]
            : throw new InvalidOperationException("Aucun résultat : le script doit afficher les données avec .Dump().");
    }

    /// <summary>
    /// Execution through Quill: cancellation returns control immediately, but the user's code cannot be
    /// interrupted in-process (infinite loop...); it finishes in the background
    /// (<see cref="CSharpRunAbandonedException"/>) and its assembly is only unloaded when it actually ends.
    /// </summary>
    /// <param name="values">Parameter values, set in QueryGlobals; null: none.</param>
    private static async Task RunScriptAsync(LinqParametersAssembly globals, string query, string connectionString, DataRunOptions options, int maxRows, IReadOnlyList<LinqParameter>? values, CancellationToken cancellationToken)
    {
        var context = LinqCompiler.CreateContext(globals.Model!, connectionString);
        var isAbandoned = false;

        try
        {
            await LinqCompiler.RunAsync(globals, query, context, options, maxRows, values, cancellationToken).ConfigureAwait(false);
        }
        catch (CSharpRunAbandonedException)
        {
            // The script is still running on this context: it keeps it (and its connection) until it ends.
            isAbandoned = true;
            throw;
        }
        finally
        {
            if (!isAbandoned)
                await context.DisposeAsync().ConfigureAwait(false);
        }
    }

    private static void LogGeneratedErrors(Action<LogEntry>? log, LinqCompilationException exception)
    {
        if (log is null)
            return;

        log(new LogEntry(LogLevel.Error, exception.Message));
        foreach (var error in exception.Errors.Take(LinqExecutionConstants.MaxGeneratedErrors))
            log(new LogEntry(LogLevel.Error, error));
    }

    /// <summary>Script output, bounded; the script may still write in the background after a cancellation.</summary>
    private sealed class LogOutput
    {
        private readonly Lock _lock = new();
        private readonly Action<LogEntry>? _log;
        private int _length;
        private bool _isTruncated;

        public LogOutput(Action<LogEntry>? log)
        {
            _log = log;
        }

        public void Write(LogEntry entry)
        {
            if (_log is null)
                return;

            var text = entry.Text;
            var truncatesNow = false;
            lock (_lock)
            {
                if (_isTruncated)
                    return;

                // The line break counts: a loop of empty messages stays bounded.
                var room = LinqExecutionConstants.MaxLogLength - _length - 1;
                if (text.Length > room)
                {
                    _isTruncated = truncatesNow = true;
                    text = text[..Math.Max(0, room)];
                }

                _length += text.Length + 1;
            }

            // Outside the lock: the receiver must not block the script's other writes.
            _log(entry with { Text = text.ReplaceLineEndings() });
            if (truncatesNow)
                _log(new LogEntry(LogLevel.Error, string.Format(CultureInfo.CurrentCulture, "Sortie Log tronquée à {0:N0} caractères.", LinqExecutionConstants.MaxLogLength)));
        }
    }
}
