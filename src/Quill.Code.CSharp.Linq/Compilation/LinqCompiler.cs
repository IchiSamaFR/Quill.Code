using Microsoft.EntityFrameworkCore;
using Quill.Code.Analysis;
using Quill.Code.CSharp.Compilation;
using Quill.Code.CSharp.Linq.Constants;
using Quill.Code.CSharp.Linq.Execution;
using Quill.Code.CSharp.Linq.Modeling;
using Quill.Code.Execution;
using Quill.Code.Scripting;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Quill.Code.CSharp.Linq.Compilation;

/// <summary>
/// Requetes LINQ via Quill : DbContext genere (une fois par schema), QueryGlobals (une fois par modele et jeu de
/// parametres), services C# de l'editeur, puis chaque requete, compilee depuis le projet meme de l'editeur.
/// Sans etat propre hormis le cache des services.
/// </summary>
[UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Compilation en memoire, sans trimming : compilation et reflexion assumees.")]
[UnconditionalSuppressMessage("Trimming", "IL2072", Justification = "Compilation en memoire, sans trimming : compilation et reflexion assumees.")]
[UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "Compilation en memoire, sans trimming : compilation et reflexion assumees.")]
[UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Compilation en memoire, sans AOT : compilation et reflexion assumees.")]
internal static class LinqCompiler
{
    // Un service par QueryGlobals compile (libere avec lui) : references, contexte et parametres ne changent pas d'une
    // frappe a l'autre ni d'une execution a l'autre (les valeurs sont posees a l'execution), Roslyn reutilise sa compilation.
    private static readonly ConditionalWeakTable<LinqParametersAssembly, CSharpLanguageService> Services = new();

    /// <summary>Service C# (Quill) partage par l'editeur et l'execution : la requete est du C# natif.</summary>
    /// <param name="parameters">QueryGlobals compile : DbContext de la connexion (ou C# seul) et parametres de la requete.</param>
    public static CSharpLanguageService GetLanguageService(LinqParametersAssembly parameters)
    {
        ArgumentNullException.ThrowIfNull(parameters);

        return Services.GetValue(parameters, CreateLanguageService);
    }

    /// <summary>Compile et charge le modele genere ; leve <see cref="LinqCompilationException"/> en cas d'echec.</summary>
    /// <param name="provider">Moteur du modele : references de son fournisseur EF, connexion du contexte.</param>
    public static async Task<LinqGeneratedModelAssembly> CompileModelAsync(EfGeneratedModel model, ILinqDatabaseProvider provider, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(provider);

        var references = LinqQuerySource.GetPlatformReferences(provider);

        // Projet a part, sans editeur : le modele est compile seul, une fois par schema.
        var service = new CSharpLanguageService(() => new CSharpLanguageOptions
        {
            Name = $"EfModel_{model.ContextClassName}",
            References = references,
            CompilationOptions = LinqQuerySource.ModelCompilationOptions,
        });

        // Non collectable : le modele vit tant que l'application, et EF garde ses types en cache.
        var compiled = await service.CompileAsync(model.Source, new CSharpCompileOptions
        {
            AssemblyName = $"EfModel_{Guid.NewGuid():N}",
            Collectible = false,
        }, cancellationToken);

        if (compiled.Assembly is not { } assembly)
            throw new LinqCompilationException($"Le modele genere {model.ContextClassName} ne compile pas.", compiled.Errors.Select(e => e.Describe(model.Source)).ToArray());

        var contextType = assembly.GetType($"{model.Namespace}.{model.ContextClassName}", throwOnError: true)!;
        var extensionsType = assembly.GetType($"{model.Namespace}.{EfModelConstants.ExtensionsClassName}", throwOnError: true)!;

        return new LinqGeneratedModelAssembly
        {
            Model = model,
            Provider = provider,
            PlatformReferences = references,
            Assembly = assembly,
            ContextType = contextType,
            DumpSinkProperty = extensionsType.GetProperty(EfModelConstants.DumpSinkPropertyName, BindingFlags.NonPublic | BindingFlags.Static)!,
            Reference = compiled.Reference!
        };
    }

    /// <summary>
    /// Compile et charge QueryGlobals (contexte et parametres, sans valeur) contre <paramref name="model"/> ;
    /// leve <see cref="LinqCompilationException"/> en cas d'echec.
    /// </summary>
    /// <param name="model">DbContext compile de la connexion, ou null : Db est un DbContext (C# seul).</param>
    /// <param name="parameters">Parametres de la requete ; seuls leurs noms et types comptent.</param>
    public static async Task<LinqParametersAssembly> CompileParametersAsync(LinqGeneratedModelAssembly? model, IReadOnlyList<LinqParameter> parameters, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(parameters);

        var source = LinqQuerySource.BuildGlobals(model?.Model, parameters);
        var service = new CSharpLanguageService(() => new CSharpLanguageOptions
        {
            Name = $"LinqParameters_{model?.Model.ContextClassName ?? LinqQueryConstants.WithoutModelName}",
            References = model is null ? LinqQuerySource.GetPlatformReferences(null) : [.. model.PlatformReferences, model.Reference],
            CompilationOptions = LinqQuerySource.ModelCompilationOptions,
        });

        // Non collectable, comme le modele : les requetes (collectables) la referencent.
        var compiled = await service.CompileAsync(source, new CSharpCompileOptions
        {
            AssemblyName = $"LinqParameters_{Guid.NewGuid():N}",
            Collectible = false,
            ResolveAssembly = name => ResolveModel(model, name),
        }, cancellationToken);

        if (compiled.Assembly is not { } assembly)
            throw new LinqCompilationException("Les parametres de la requete ne compilent pas.", compiled.Errors.Select(e => e.Describe(source)).ToArray());

        var globalsType = assembly.GetType(LinqQueryConstants.GlobalsClass, throwOnError: true)!;

        return new LinqParametersAssembly
        {
            Model = model,
            Assembly = assembly,
            ContextProperty = globalsType.GetProperty(LinqQueryConstants.ContextGlobal, LinqQueryConstants.StaticMembers)!,
            ParameterProperties = LinqQuerySource.GetGlobalParameters(parameters)
                .ToDictionary(p => p.Name, p => globalsType.GetProperty(p.Name, LinqQueryConstants.StaticMembers)!, StringComparer.Ordinal),
            Reference = compiled.Reference!,
        };
    }

    /// <summary>
    /// Compile une requete (C# natif, instructions de haut niveau) depuis le projet que l'editeur analyse, sans
    /// l'executer ; les erreurs sont dans le resultat, pas levees.
    /// </summary>
    /// <param name="log">Recoit les Log.* de la requete (runtime de script de Quill).</param>
    public static Task<CSharpCompilationResult> CompileQueryAsync(LinqParametersAssembly parameters, string query, Action<LogEntry>? log = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        ArgumentNullException.ThrowIfNull(query);

        // Projet meme de l'editeur (references, contexte genere, parametres) : ce qui est souligne est ce qui echoue.
        return GetLanguageService(parameters).CompileAsync(query, QueryOptions(parameters, log), cancellationToken);
    }

    /// <summary>
    /// Compile et execute la requete sur <paramref name="context"/> en un appel (<see cref="CSharpLanguageService.RunAsync"/>
    /// de Quill), depuis le projet que l'editeur analyse : chaque Dump(valeur, titre) est converti en jeu de resultats
    /// (<paramref name="maxRows"/> lignes au plus) et remis par <see cref="DataRunOptions.Data"/> ; le SQL genere d'une
    /// requete EF et les Log.* passent par <see cref="RunOptions.Log"/>.
    /// </summary>
    /// <param name="values">Valeurs des parametres, par nom ; un parametre absent vaut null.</param>
    /// <exception cref="CSharpCompilationException">La requete ne compile pas ; rien n'a ete execute.</exception>
    /// <exception cref="LinqCompilationException">Une valeur de parametre ne convient pas a son type ; rien n'a ete execute.</exception>
    public static Task RunAsync(LinqParametersAssembly parameters, string query, DbContext context, DataRunOptions options, int maxRows, IReadOnlyList<LinqParameter>? values = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);

        var index = 0;
        return RunAsync(parameters, query, context, (Action<object?, string?>?)((value, title) =>
        {
            var (resultSet, sql) = LinqResultConverter.Capture(value, ++index, title, maxRows, cancellationToken);
            if (sql is not null)
                options.Log?.Invoke(new LogEntry(LogLevel.Information, $"{resultSet.Title} : SQL généré :{Environment.NewLine}{sql}"));

            options.Data?.Invoke(resultSet);
        }), values, options, cancellationToken);
    }

    /// <summary>
    /// Compile et execute la requete sur <paramref name="context"/> en un appel (<see cref="CSharpLanguageService.RunAsync"/>
    /// de Quill) : chaque Dump(valeur, titre) est remis tel quel a <paramref name="dump"/>, les Log.* par
    /// <see cref="RunOptions.Log"/>.
    /// </summary>
    /// <param name="values">Valeurs des parametres, par nom ; un parametre absent vaut null.</param>
    /// <exception cref="CSharpCompilationException">La requete ne compile pas ; rien n'a ete execute.</exception>
    /// <exception cref="LinqCompilationException">Une valeur de parametre ne convient pas a son type ; rien n'a ete execute.</exception>
    /// <exception cref="CSharpRunAbandonedException">
    /// Annulation : la main est rendue tout de suite, le script se termine en arriere-plan. Une exception du script est
    /// relevee telle quelle.
    /// </exception>
    public static async Task RunAsync(LinqParametersAssembly parameters, string query, DbContext context, Action<object?, string?>? dump, IReadOnlyList<LinqParameter>? values = null, RunOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(context);

        var model = parameters.Model
            ?? throw new InvalidOperationException("Une requete ne s'execute que sur le modele d'une connexion.");

        // Toutes les valeurs sont converties avant d'en poser une : une erreur n'en laisse aucune a moitie posee.
        Dictionary<PropertyInfo, object?> converted = [];
        try
        {
            foreach (var (name, parameter) in LinqQuerySource.GetGlobalParameters(values ?? []))
            {
                if (parameters.ParameterProperties.TryGetValue(name, out var property))
                    converted[property] = LinqQuerySource.ConvertValue(name, parameter);
            }
        }
        catch (ArgumentException ex)
        {
            throw new LinqCompilationException(ex.Message, []);
        }

        // Proprietes en AsyncLocal, posees dans ce flux avant le depart du script : Dump, Db puis les parametres.
        model.DumpSinkProperty.SetValue(null, dump);
        parameters.ContextProperty.SetValue(null, context);
        foreach (var (property, value) in converted)
            property.SetValue(null, value);

        await GetLanguageService(parameters).RunAsync(query, QueryOptions(parameters, options?.Log), cancellationToken);
    }

    /// <summary>Contexte EF du modele sur une chaine de connexion de son moteur.</summary>
    public static DbContext CreateContext(LinqGeneratedModelAssembly model, string connectionString)
    {
        ArgumentNullException.ThrowIfNull(model);

        var builder = (DbContextOptionsBuilder)Activator.CreateInstance(typeof(DbContextOptionsBuilder<>).MakeGenericType(model.ContextType))!;
        model.Provider.Configure(builder, connectionString);

        // Lecture seule : pas de suivi des entites, inutile pour afficher des resultats.
        builder.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);

        return (DbContext)Activator.CreateInstance(model.ContextType, builder.Options)!;
    }

    /// <summary>Options d'une requete : modele et parametres servis tels quels (EF garde ses types en cache), le reste vient du runtime.</summary>
    private static CSharpCompileOptions QueryOptions(LinqParametersAssembly parameters, Action<LogEntry>? log) => new()
    {
        AssemblyName = $"LinqQuery_{Guid.NewGuid():N}",
        ResolveAssembly = name => AssemblyName.ReferenceMatchesDefinition(name, parameters.Assembly.GetName())
            ? parameters.Assembly
            : ResolveModel(parameters.Model, name),
        Log = log,
    };

    private static Assembly? ResolveModel(LinqGeneratedModelAssembly? model, AssemblyName name)
        => model is not null && AssemblyName.ReferenceMatchesDefinition(name, model.Assembly.GetName()) ? model.Assembly : null;

    /// <summary>Projet cree par Quill a sa premiere utilisation : le declarer ne coute rien.</summary>
    private static CSharpLanguageService CreateLanguageService(LinqParametersAssembly parameters)
    {
        return new(() => new CSharpLanguageOptions
        {
            Name = $"Linq_{parameters.Model?.Model.ContextClassName ?? LinqQueryConstants.WithoutModelName}",
            References = parameters.Model is null
                ? [.. LinqQuerySource.GetPlatformReferences(null), parameters.Reference]
                : [.. parameters.Model.PlatformReferences, parameters.Model.Reference, parameters.Reference],
            DocumentName = LinqQueryConstants.QueryDocumentName,

            // Usings implicites, alias QueryDbContext et import de QueryGlobals : fixes, Roslyn ne les reanalyse pas a
            // chaque frappe. Dump vient du modele, Db et les parametres de QueryGlobals, Log et Script du runtime de Quill.
            AdditionalDocuments = new Dictionary<string, string>
            {
                [LinqQueryConstants.GlobalUsings] = LinqQuerySource.BuildGlobalUsings(parameters.Model?.Model),
            },

            // Sans DbContext, les tables n'existent pas : seules les fautes de syntaxe ont un sens.
            IncludeSemanticDiagnostics = parameters.Model is not null,
        });
    }
}
