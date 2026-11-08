using Quill.Code.CSharp.Linq.Execution;
using Quill.Code.CSharp.Linq.Modeling;
using Quill.Data.Sql;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace Quill.Code.CSharp.Linq.Compilation;

/// <summary>
/// LINQ context of a database: DbContext generated then compiled (once per structure read), QueryGlobals compiled (once
/// per model and set of parameters), and C# service placing the query in that context. Shared by
/// <see cref="LinqLanguageService"/> and <see cref="LinqQueryExecutor"/>: the first one pays for the generation.
/// A cache, not a registry: each entry lives as long as the structure (or model) holding it.
/// </summary>
internal static class LinqContexts
{
    /// <summary>One model per provider and per structure read: reading the database again (new instance) recompiles the model.</summary>
    private static readonly ConditionalWeakTable<ILinqDatabaseProvider, ConditionalWeakTable<SqlDatabaseSchema, Task<LinqGeneratedModelAssembly>>> Models = new();

    /// <summary>One QueryGlobals per model (released with it) and per set of parameters (generated source, without values).</summary>
    private static readonly ConditionalWeakTable<LinqGeneratedModelAssembly, ConcurrentDictionary<string, Task<LinqParametersAssembly>>> Parameters = new();

    /// <summary>QueryGlobals without a model (C# only), per set of parameters.</summary>
    private static readonly ConcurrentDictionary<string, Task<LinqParametersAssembly>> ParametersWithoutModel = new();

    /// <summary>Model of <paramref name="schema"/> on <paramref name="provider"/>; throws <see cref="LinqCompilationException"/> if it does not compile.</summary>
    public static async Task<LinqGeneratedModelAssembly> GetModelAsync(ILinqDatabaseProvider provider, SqlDatabaseSchema schema, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(provider);
        ArgumentNullException.ThrowIfNull(schema);

        var models = Models.GetValue(provider, static _ => new());
        ConditionalWeakTable<SqlDatabaseSchema, Task<LinqGeneratedModelAssembly>>.CreateValueCallback compile = s => Compile(s, provider);

        var task = models.GetValue(schema, compile);
        if (task.IsFaulted)
        {
            // Un echec n'est pas garde : la prochaine demande retente la compilation.
            models.Remove(schema);
            task = models.GetValue(schema, compile);
        }

        // Compilation partagee : l'annulation d'un appelant ne l'interrompt pas.
        return await task.WaitAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// QueryGlobals of <paramref name="model"/> (null = C# only) and of the <paramref name="parameters"/> (names and types; the
    /// values are set at execution); throws <see cref="LinqCompilationException"/> on failure.
    /// </summary>
    public static async Task<LinqParametersAssembly> GetParametersAsync(LinqGeneratedModelAssembly? model, IReadOnlyList<LinqParameter>? parameters = null, CancellationToken cancellationToken = default)
    {
        parameters ??= [];

        // Le source genere sert de cle : memes noms et types, meme assembly, quelles que soient les valeurs.
        var key = LinqQuerySource.BuildGlobals(model?.Model, parameters);
        var entries = model is null ? ParametersWithoutModel : Parameters.GetValue(model, static _ => new());

        var task = entries.GetOrAdd(key, _ => Compile(model, parameters));
        if (task.IsFaulted)
        {
            // Un echec n'est pas garde : la prochaine demande retente la compilation.
            entries.TryRemove(new KeyValuePair<string, Task<LinqParametersAssembly>>(key, task));
            task = entries.GetOrAdd(key, _ => Compile(model, parameters));
        }

        // Compilation partagee : l'annulation d'un appelant ne l'interrompt pas.
        return await task.WaitAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// C# service for an editor request: with the DbContext of <paramref name="schema"/> if it compiles, otherwise C# only.
    /// </summary>
    /// <param name="provider">Database engine; null: C# only.</param>
    /// <param name="schema">Database structure; null: C# only (no connection, unreachable database).</param>
    /// <param name="parameters">Query parameters, used by the script as variables; null: none.</param>
    public static async Task<CSharpLanguageService> GetLanguageServiceAsync(ILinqDatabaseProvider? provider, SqlDatabaseSchema? schema, IReadOnlyList<LinqParameter>? parameters = null, CancellationToken cancellationToken = default)
    {
        var model = await TryGetModelAsync(provider, schema, cancellationToken).ConfigureAwait(false);

        LinqParametersAssembly globals;
        try
        {
            globals = await GetParametersAsync(model, parameters, cancellationToken).ConfigureAwait(false);
        }
        catch (LinqCompilationException)
        {
            // Un parametre au type inutilisable ne doit pas priver l'editeur du contexte.
            globals = await GetParametersAsync(model, [], cancellationToken).ConfigureAwait(false);
        }

        return LinqCompiler.GetLanguageService(globals);
    }

    /// <summary>DbContext of the database, or null: analysis falls back to C# only.</summary>
    private static async Task<LinqGeneratedModelAssembly?> TryGetModelAsync(ILinqDatabaseProvider? provider, SqlDatabaseSchema? schema, CancellationToken cancellationToken)
    {
        if (provider is null || schema is null)
            return null;

        try
        {
            return await GetModelAsync(provider, schema, cancellationToken).ConfigureAwait(false);
        }
        catch (LinqCompilationException)
        {
            return null;
        }
    }

    private static Task<LinqGeneratedModelAssembly> Compile(SqlDatabaseSchema schema, ILinqDatabaseProvider provider)
        => Task.Run(() => LinqCompiler.CompileModelAsync(EfModelGenerator.Generate(schema, provider), provider));

    private static Task<LinqParametersAssembly> Compile(LinqGeneratedModelAssembly? model, IReadOnlyList<LinqParameter> parameters)
        => Task.Run(() => LinqCompiler.CompileParametersAsync(model, parameters));
}
