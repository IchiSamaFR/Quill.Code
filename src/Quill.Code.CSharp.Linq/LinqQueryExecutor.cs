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
/// Execution LINQ sur la base d'un <see cref="ILinqDatabaseProvider"/>, sur le modele de l'execution SQL de <c>Quill.Code.Sql.SqlServer</c> : <see cref="RunAsync"/>
/// ne rend rien et remet tout au fil de l'eau (chaque <c>Dump(valeur, titre)</c> par Data, SQL genere, <c>Log.*</c> et
/// erreurs par Log), <see cref="ReadAsync"/> rend un jeu et leve ses erreurs. La requete est compilee contre le
/// DbContext genere depuis la structure de la base, dans le projet meme de <see cref="LinqLanguageService"/>.
/// Un contexte EF est cree pour chaque appel.
/// </summary>
public sealed class LinqQueryExecutor
{
    private readonly ILinqDatabaseProvider _provider;

    /// <summary>Executeur LINQ sur le moteur de <paramref name="provider"/> (meme instance que l'editeur : meme DbContext compile).</summary>
    public LinqQueryExecutor(ILinqDatabaseProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);
        _provider = provider;
    }

    /// <summary>
    /// Execute la requete sans rien rendre : tout est remis au fil de l'eau par <paramref name="options"/>. Une erreur de
    /// compilation ou une exception du script ne leve pas d'exception : elle est remise par <see cref="RunOptions.Log"/>
    /// au niveau Error.
    /// </summary>
    /// <param name="schema">Structure de la base de <paramref name="connectionString"/> ; une meme instance donne le meme DbContext compile.</param>
    /// <param name="options">
    /// <see cref="RunOptions.Log"/> : contexte, SQL genere et lignes lues (Information, Host), <c>Log.*</c> du script (Code),
    /// erreurs (Error, Code pour la requete, Host pour le code genere) ;
    /// <see cref="DataRunOptions.Data"/> : chaque Dump, converti en jeu de resultats des son appel.
    /// </param>
    /// <exception cref="OperationCanceledException">
    /// Execution annulee ; <see cref="CSharpRunAbandonedException"/> quand le script, qui ne peut pas etre interrompu,
    /// se termine en arriere-plan (il garde son contexte EF jusque-la).
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
            // Comme SQL Server : l'annulation d'une commande en cours remonte sous forme d'erreur du pilote.
            throw new OperationCanceledException(ex.Message, ex, cancellationToken);
        }
        catch (DbException ex)
        {
            log?.Invoke(new LogEntry(LogLevel.Error, ex.Message, LogSource.Code));
            return;
        }
#pragma warning disable CA1031 // Le script est du code de l'utilisateur : toute exception (traduction EF, null, division...) est un resultat.
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
            // Plusieurs jeux : chaque message dit a quel onglet il se rapporte.
            var prefix = dumped.Length > 1 ? $"{resultSet.Title} : " : string.Empty;
            log?.Invoke(new LogEntry(LogLevel.Information, string.Create(CultureInfo.InvariantCulture, $"{prefix}({resultSet.Rows.Count} ligne(s))")));
        }

        if (dumped.Length == 0)
            log?.Invoke(new LogEntry(LogLevel.Information, "Aucun résultat : affichez les données avec .Dump()."));
    }

    /// <summary>
    /// Jeu du dernier Dump d'une requete parametree (au plus <paramref name="maxRows"/> lignes) ;
    /// <paramref name="database"/> null = base de la chaine de connexion. Une erreur leve une exception.
    /// </summary>
    /// <param name="schema">Structure de la base interrogee ; une meme instance donne le meme DbContext compile.</param>
    /// <param name="parameters">Valeurs utilisables par leur nom (sans @), envoyees par EF en parametres SQL.</param>
    /// <exception cref="InvalidOperationException">La requete ne compile pas, une valeur ne convient pas a son type, ou rien n'est affiche.</exception>
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

        // Une lecture n'affiche qu'un jeu : le dernier Dump().
        return resultSets.Count > 0
            ? resultSets[^1]
            : throw new InvalidOperationException("Aucun résultat : le script doit afficher les données avec .Dump().");
    }

    /// <summary>
    /// Execution par Quill : l'annulation rend la main tout de suite, mais le code de l'utilisateur ne peut pas etre
    /// interrompu dans le processus (boucle infinie...) ; il se termine en arriere-plan
    /// (<see cref="CSharpRunAbandonedException"/>) et son assembly n'est dechargee qu'a sa fin reelle.
    /// </summary>
    /// <param name="values">Valeurs des parametres, posees dans QueryGlobals ; null : aucun.</param>
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
            // Le script tourne encore sur ce contexte : il le garde (et sa connexion) jusqu'a sa fin.
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

    /// <summary>Sortie du script, bornee ; le script peut encore ecrire en arriere-plan apres une annulation.</summary>
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

                // Le saut de ligne compte : une boucle de messages vides reste bornee.
                var room = LinqExecutionConstants.MaxLogLength - _length - 1;
                if (text.Length > room)
                {
                    _isTruncated = truncatesNow = true;
                    text = text[..Math.Max(0, room)];
                }

                _length += text.Length + 1;
            }

            // Hors verrou : le destinataire ne doit pas bloquer les autres ecritures du script.
            _log(entry with { Text = text.ReplaceLineEndings() });
            if (truncatesNow)
                _log(new LogEntry(LogLevel.Error, string.Format(CultureInfo.CurrentCulture, "Sortie Log tronquée à {0:N0} caractères.", LinqExecutionConstants.MaxLogLength)));
        }
    }
}
