using Quill.Code.CSharp.Linq.Execution;
using Quill.Code.CSharp.Linq.Modeling;
using Quill.Data.Sql;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace Quill.Code.CSharp.Linq.Compilation;

/// <summary>
/// Contexte LINQ d'une base : DbContext genere puis compile (une fois par structure lue), QueryGlobals compile (une fois
/// par modele et jeu de parametres), et service C# qui place la requete dans ce contexte. Partage par
/// <see cref="LinqLanguageService"/> et <see cref="LinqQueryExecutor"/> : le premier paie la generation.
/// Cache, pas registre : chaque entree vit tant que la structure (ou le modele) qui la porte.
/// </summary>
internal static class LinqContexts
{
    /// <summary>Un modele par fournisseur et par structure lue : relire la base (nouvelle instance) recompile le modele.</summary>
    private static readonly ConditionalWeakTable<ILinqDatabaseProvider, ConditionalWeakTable<SqlDatabaseSchema, Task<LinqGeneratedModelAssembly>>> Models = new();

    /// <summary>Un QueryGlobals par modele (libere avec lui) et par jeu de parametres (source genere, sans valeur).</summary>
    private static readonly ConditionalWeakTable<LinqGeneratedModelAssembly, ConcurrentDictionary<string, Task<LinqParametersAssembly>>> Parameters = new();

    /// <summary>QueryGlobals sans modele (C# seul), par jeu de parametres.</summary>
    private static readonly ConcurrentDictionary<string, Task<LinqParametersAssembly>> ParametersWithoutModel = new();

    /// <summary>Modele de <paramref name="schema"/> sur <paramref name="provider"/> ; leve <see cref="LinqCompilationException"/> s'il ne compile pas.</summary>
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
    /// QueryGlobals de <paramref name="model"/> (null = C# seul) et des <paramref name="parameters"/> (noms et types ; les
    /// valeurs sont posees a l'execution) ; leve <see cref="LinqCompilationException"/> en cas d'echec.
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
    /// Service C# d'une demande de l'editeur : avec le DbContext de <paramref name="schema"/> s'il compile, sinon C# seul.
    /// </summary>
    /// <param name="provider">Moteur de la base ; null : C# seul.</param>
    /// <param name="schema">Structure de la base ; null : C# seul (pas de connexion, base injoignable).</param>
    /// <param name="parameters">Parametres de la requete, que le script utilise comme variables ; null : aucun.</param>
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

    /// <summary>DbContext de la base, ou null : l'analyse se rabat sur le C# seul.</summary>
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
