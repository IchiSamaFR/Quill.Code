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
/// LINQ queries through Quill: generated DbContext (once per schema), QueryGlobals (once per model and set of
/// parameters), editor C# services, then each query, compiled from the very project of the editor.
/// No state of its own apart from the service cache.
/// </summary>
[UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Compilation en memoire, sans trimming : compilation et reflexion assumees.")]
[UnconditionalSuppressMessage("Trimming", "IL2072", Justification = "Compilation en memoire, sans trimming : compilation et reflexion assumees.")]
[UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "Compilation en memoire, sans trimming : compilation et reflexion assumees.")]
[UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Compilation en memoire, sans AOT : compilation et reflexion assumees.")]
internal static class LinqCompiler
{
    // One service per compiled QueryGlobals (released with it): references, context and parameters do not change from one
    // keystroke to the next nor from one execution to the next (values are set at execution), Roslyn reuses its compilation.
    private static readonly ConditionalWeakTable<LinqParametersAssembly, CSharpLanguageService> Services = new();

    /// <summary>C# service (Quill) shared by the editor and the execution: the query is native C#.</summary>
    /// <param name="parameters">Compiled QueryGlobals: DbContext of the connection (or C# only) and query parameters.</param>
    public static CSharpLanguageService GetLanguageService(LinqParametersAssembly parameters)
    {
        ArgumentNullException.ThrowIfNull(parameters);

        return Services.GetValue(parameters, CreateLanguageService);
    }

    /// <summary>Compiles and loads the generated model; throws <see cref="LinqCompilationException"/> on failure.</summary>
    /// <param name="provider">Engine of the model: references of its EF provider, context connection.</param>
    public static async Task<LinqGeneratedModelAssembly> CompileModelAsync(EfGeneratedModel model, ILinqDatabaseProvider provider, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(provider);

        var references = LinqQuerySource.GetPlatformReferences(provider);

        // Separate project, without an editor: the model is compiled alone, once per schema.
        var service = new CSharpLanguageService(() => new CSharpLanguageOptions
        {
            Name = $"EfModel_{model.ContextClassName}",
            References = references,
            CompilationOptions = LinqQuerySource.ModelCompilationOptions,
        });

        // Not collectible: the model lives as long as the application, and EF caches its types.
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
    /// Compiles and loads QueryGlobals (context and parameters, without values) against <paramref name="model"/>;
    /// throws <see cref="LinqCompilationException"/> on failure.
    /// </summary>
    /// <param name="model">Compiled DbContext of the connection, or null: Db is a DbContext (C# only).</param>
    /// <param name="parameters">Query parameters; only their names and types matter.</param>
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

        // Not collectible, like the model: the (collectible) queries reference it.
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
    /// Compiles a query (native C#, top-level statements) from the project the editor analyzes, without
    /// running it; errors are in the result, not thrown.
    /// </summary>
    /// <param name="log">Receives the query's Log.* (Quill script runtime).</param>
    public static Task<CSharpCompilationResult> CompileQueryAsync(LinqParametersAssembly parameters, string query, Action<LogEntry>? log = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        ArgumentNullException.ThrowIfNull(query);

        // The editor's very project (references, generated context, parameters): what is underlined is what fails.
        return GetLanguageService(parameters).CompileAsync(query, QueryOptions(parameters, log), cancellationToken);
    }

    /// <summary>
    /// Compiles and runs the query on <paramref name="context"/> in one call (Quill's <see cref="CSharpLanguageService.RunAsync"/>),
    /// from the project the editor analyzes: each Dump(value, title) is converted to a result set
    /// (at most <paramref name="maxRows"/> rows) and handed over through <see cref="DataRunOptions.Data"/>; the generated SQL of an
    /// EF query and the Log.* go through <see cref="RunOptions.Log"/>.
    /// </summary>
    /// <param name="values">Parameter values, by name; a missing parameter is null.</param>
    /// <exception cref="CSharpCompilationException">The query does not compile; nothing was run.</exception>
    /// <exception cref="LinqCompilationException">A parameter value does not fit its type; nothing was run.</exception>
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
    /// Compiles and runs the query on <paramref name="context"/> in one call (Quill's <see cref="CSharpLanguageService.RunAsync"/>):
    /// each Dump(value, title) is handed as is to <paramref name="dump"/>, the Log.* through
    /// <see cref="RunOptions.Log"/>.
    /// </summary>
    /// <param name="values">Parameter values, by name; a missing parameter is null.</param>
    /// <exception cref="CSharpCompilationException">The query does not compile; nothing was run.</exception>
    /// <exception cref="LinqCompilationException">A parameter value does not fit its type; nothing was run.</exception>
    /// <exception cref="CSharpRunAbandonedException">
    /// Cancellation: control returns immediately, the script finishes in the background. An exception from the script is
    /// rethrown as is.
    /// </exception>
    public static async Task RunAsync(LinqParametersAssembly parameters, string query, DbContext context, Action<object?, string?>? dump, IReadOnlyList<LinqParameter>? values = null, RunOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(context);

        var model = parameters.Model
            ?? throw new InvalidOperationException("Une requete ne s'execute que sur le modele d'une connexion.");

        // All values are converted before setting any: an error leaves none half set.
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

        // AsyncLocal properties, set in this flow before the script starts: Dump, Db then the parameters.
        model.DumpSinkProperty.SetValue(null, dump);
        parameters.ContextProperty.SetValue(null, context);
        foreach (var (property, value) in converted)
            property.SetValue(null, value);

        await GetLanguageService(parameters).RunAsync(query, QueryOptions(parameters, options?.Log), cancellationToken);
    }

    /// <summary>EF context of the model on a connection string of its engine.</summary>
    public static DbContext CreateContext(LinqGeneratedModelAssembly model, string connectionString)
    {
        ArgumentNullException.ThrowIfNull(model);

        var builder = (DbContextOptionsBuilder)Activator.CreateInstance(typeof(DbContextOptionsBuilder<>).MakeGenericType(model.ContextType))!;
        model.Provider.Configure(builder, connectionString);

        // Read-only: no entity tracking, useless for displaying results.
        builder.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);

        return (DbContext)Activator.CreateInstance(model.ContextType, builder.Options)!;
    }

    /// <summary>Options of a query: model and parameters served as is (EF caches its types), the rest comes from the runtime.</summary>
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

    /// <summary>Project created by Quill on first use: declaring it costs nothing.</summary>
    private static CSharpLanguageService CreateLanguageService(LinqParametersAssembly parameters)
    {
        return new(() => new CSharpLanguageOptions
        {
            Name = $"Linq_{parameters.Model?.Model.ContextClassName ?? LinqQueryConstants.WithoutModelName}",
            References = parameters.Model is null
                ? [.. LinqQuerySource.GetPlatformReferences(null), parameters.Reference]
                : [.. parameters.Model.PlatformReferences, parameters.Model.Reference, parameters.Reference],
            DocumentName = LinqQueryConstants.QueryDocumentName,

            // Implicit usings, QueryDbContext alias and QueryGlobals import: fixed, Roslyn does not reanalyze them on
            // every keystroke. Dump comes from the model, Db and the parameters from QueryGlobals, Log and Script from the Quill runtime.
            AdditionalDocuments = new Dictionary<string, string>
            {
                [LinqQueryConstants.GlobalUsings] = LinqQuerySource.BuildGlobalUsings(parameters.Model?.Model),
            },

            // Without a DbContext, tables do not exist: only syntax errors make sense.
            IncludeSemanticDiagnostics = parameters.Model is not null,
        });
    }
}
