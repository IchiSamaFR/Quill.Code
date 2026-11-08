using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.EntityFrameworkCore;
using Quill.Code.CSharp.Linq.Execution;
using Quill.Code.CSharp.Linq.Modeling;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using static Quill.Code.CSharp.Linq.Constants.LinqQueryConstants;

namespace Quill.Code.CSharp.Linq.Compilation;

/// <summary>
/// C# frame of LINQ queries (stateless): usings and context alias, globals (context and parameters), references
/// and compilation options. Shared by the editor and the execution.
/// </summary>
[UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Compilation en memoire : reflexion assumee, pas de publication trimmed.")]
internal static class LinqQuerySource
{
    private static readonly Dictionary<Type, string> TypeKeywords = new()
    {
        [typeof(int)] = "int",
        [typeof(long)] = "long",
        [typeof(short)] = "short",
        [typeof(byte)] = "byte",
        [typeof(bool)] = "bool",
        [typeof(decimal)] = "decimal",
        [typeof(double)] = "double",
        [typeof(float)] = "float",
        [typeof(string)] = "string",
        [typeof(object)] = "object",
    };

    /// <summary>
    /// Platform and EF assemblies compiled against, taken from the loaded runtime
    /// (like LINQPad): no reference packs to ship. Expensive lists: computed once,
    /// without a provider (C# only) then per provider.
    /// </summary>
    private static readonly Lazy<IReadOnlyList<MetadataReference>> NeutralReferences = new(() => LoadPlatformReferences(null));

    private static readonly ConditionalWeakTable<ILinqDatabaseProvider, IReadOnlyList<MetadataReference>> ProviderReferences = new();

    private static readonly string[] ReferencedAssemblyPrefixes =
    [
        "System.", "mscorlib", "netstandard", "Microsoft.CSharp", "Microsoft.EntityFrameworkCore", "Microsoft.Extensions."
    ];

    /// <summary>References shared by compilation and completion; <paramref name="provider"/> null: without a provider.</summary>
    internal static IReadOnlyList<MetadataReference> GetPlatformReferences(ILinqDatabaseProvider? provider)
        => provider is null ? NeutralReferences.Value : ProviderReferences.GetValue(provider, LoadPlatformReferences);

    /// <summary>The generated model has no entry point: a library.</summary>
    internal static CSharpCompilationOptions ModelCompilationOptions { get; } = new(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Disable);

    /// <summary>
    /// Source of <see cref="LinqParametersAssembly"/>: <see cref="GlobalsClass"/> with the context and one member per parameter,
    /// without values (AsyncLocal properties, set before each execution). Also serves as cache key: same names and
    /// types, same assembly.
    /// </summary>
    internal static string BuildGlobals(EfGeneratedModel? model, IReadOnlyList<LinqParameter> parameters)
    {
        // Full type: this project does not have the QueryDbContext alias, specific to the queries' project.
        var contextType = model is null
            ? "global::Microsoft.EntityFrameworkCore.DbContext"
            : $"global::{model.Namespace}.{model.ContextClassName}";

        var code = new StringBuilder();
        code.AppendLine($"public static class {GlobalsClass}");
        code.AppendLine("{");
        AppendGlobal(code, contextType, ContextGlobal);

        foreach (var (name, parameter) in GetGlobalParameters(parameters))
            AppendGlobal(code, ToCSharpName(parameter.Type), name);

        code.AppendLine("}");
        return code.ToString();
    }

    /// <summary>AsyncLocal property: two simultaneous executions share the assembly, not their values.</summary>
    private static void AppendGlobal(StringBuilder code, string type, string name)
    {
        code.AppendLine($"    private static readonly global::System.Threading.AsyncLocal<{type}> __{name} = new();");
        code.AppendLine($"    public static {type} {name} {{ get => __{name}.Value; internal set => __{name}.Value = value; }}");
    }

    /// <summary>
    /// Parameters exposed by <see cref="GlobalsClass"/>, under their name without @. A name that is not a C# identifier,
    /// a reserved word or a duplicate is not exposed: the script cannot name it.
    /// </summary>
    internal static IReadOnlyList<(string Name, LinqParameter Parameter)> GetGlobalParameters(IReadOnlyList<LinqParameter> parameters)
    {
        var used = new HashSet<string>(StringComparer.Ordinal) { ContextGlobal, GlobalsClass };
        List<(string, LinqParameter)> globals = [];

        foreach (var parameter in parameters)
        {
            var name = parameter.Name.TrimStart('@');
            if (SyntaxFacts.IsValidIdentifier(name) && SyntaxFacts.GetKeywordKind(name) == SyntaxKind.None && used.Add(name))
                globals.Add((name, parameter));
        }

        return globals;
    }

    /// <summary>Parameter value converted to its type, null without a value. A value that does not fit the type throws <see cref="ArgumentException"/>.</summary>
    internal static object? ConvertValue(string name, LinqParameter parameter)
    {
        if (parameter.Value is null)
            return null;

        var type = Nullable.GetUnderlyingType(parameter.Type) ?? parameter.Type;

        try
        {
            return parameter.Value switch
            {
                _ when type.IsInstanceOfType(parameter.Value) => parameter.Value,
                string text when type == typeof(Guid) => Guid.Parse(text),
                DateTime date when type == typeof(DateOnly) => DateOnly.FromDateTime(date),
                _ when type.IsEnum => Enum.ToObject(type, parameter.Value),
                _ => Convert.ChangeType(parameter.Value, type, CultureInfo.InvariantCulture),
            };
        }
        catch (Exception ex) when (ex is InvalidCastException or FormatException or OverflowException or ArgumentException)
        {
            throw new ArgumentException($"Parametre « {name} » : la valeur « {parameter.Value} » ne convient pas au type {type.Name}.", nameof(parameter), ex);
        }
    }

    private static string ToCSharpName(Type type)
    {
        if (Nullable.GetUnderlyingType(type) is { } underlying)
            return ToCSharpName(underlying) + "?";

        return TypeKeywords.TryGetValue(type, out var keyword) ? keyword : $"global::{type.FullName}";
    }

    /// <summary>Implicit usings and context alias, fixed document of the project; the query can declare its own.</summary>
    internal static string BuildGlobalUsings(EfGeneratedModel? model)
    {
        var code = new StringBuilder();
        code.AppendLine("global using System;");
        code.AppendLine("global using System.Collections.Generic;");
        code.AppendLine("global using System.Linq;");
        code.AppendLine("global using System.Threading.Tasks;");
        code.AppendLine("global using Microsoft.EntityFrameworkCore;");
        code.AppendLine($"global using static {GlobalsClass};");

        // Without a connection, the alias designates DbContext: the query stays analyzable.
        if (model is null)
        {
            code.AppendLine($"global using {ContextAlias} = global::Microsoft.EntityFrameworkCore.DbContext;");
        }
        else
        {
            code.AppendLine($"global using {model.Namespace};");
            code.AppendLine($"global using {ContextAlias} = global::{model.Namespace}.{model.ContextClassName};");
        }

        return code.ToString();
    }

    private static IReadOnlyList<MetadataReference> LoadPlatformReferences(ILinqDatabaseProvider? provider)
    {
        // Runtime assemblies (otherwise, those already loaded), filtered.
        var paths = CSharpReferences.GetTrustedPlatformAssemblyPaths()
            .Where(path => ReferencedAssemblyPrefixes.Any(p => Path.GetFileName(path).StartsWith(p, StringComparison.OrdinalIgnoreCase)));

        // EF and the provider may not be loaded yet: their assemblies are forced into the list.
        string[] required = [typeof(DbContext).Assembly.Location, typeof(RelationalQueryableExtensions).Assembly.Location, .. (provider?.Assemblies ?? []).Select(a => a.Location)];

        return paths.Concat(required)
                    .Where(path => !string.IsNullOrEmpty(path))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Select(path => (MetadataReference)MetadataReference.CreateFromFile(path))
                    .ToArray();
    }
}

