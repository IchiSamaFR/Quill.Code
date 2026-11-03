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
/// Cadre C# des requetes LINQ (sans etat) : usings et alias du contexte, globals (contexte et parametres), references
/// et options de compilation. Partage par l'editeur et l'execution.
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
    /// Assemblies de la plateforme et d'EF contre lesquelles on compile, prises dans le runtime
    /// charge (comme LINQPad) : pas de packs de reference a embarquer. Listes couteuses : calculees une fois,
    /// sans fournisseur (C# seul) puis par fournisseur.
    /// </summary>
    private static readonly Lazy<IReadOnlyList<MetadataReference>> NeutralReferences = new(() => LoadPlatformReferences(null));

    private static readonly ConditionalWeakTable<ILinqDatabaseProvider, IReadOnlyList<MetadataReference>> ProviderReferences = new();

    private static readonly string[] ReferencedAssemblyPrefixes =
    [
        "System.", "mscorlib", "netstandard", "Microsoft.CSharp", "Microsoft.EntityFrameworkCore", "Microsoft.Extensions."
    ];

    /// <summary>References communes a la compilation et a l'autocompletion ; <paramref name="provider"/> null : sans fournisseur.</summary>
    internal static IReadOnlyList<MetadataReference> GetPlatformReferences(ILinqDatabaseProvider? provider)
        => provider is null ? NeutralReferences.Value : ProviderReferences.GetValue(provider, LoadPlatformReferences);

    /// <summary>Le modele genere n'a pas de point d'entree : une bibliotheque.</summary>
    internal static CSharpCompilationOptions ModelCompilationOptions { get; } = new(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Disable);

    /// <summary>
    /// Source de <see cref="LinqParametersAssembly"/> : <see cref="GlobalsClass"/> avec le contexte et un membre par parametre,
    /// sans valeur (proprietes en AsyncLocal, posees avant chaque execution). Sert aussi de cle de cache : memes noms et
    /// types, meme assembly.
    /// </summary>
    internal static string BuildGlobals(EfGeneratedModel? model, IReadOnlyList<LinqParameter> parameters)
    {
        // Type complet : ce projet n'a pas l'alias QueryDbContext, propre a celui des requetes.
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

    /// <summary>Propriete en AsyncLocal : deux executions simultanees partagent l'assembly, pas leurs valeurs.</summary>
    private static void AppendGlobal(StringBuilder code, string type, string name)
    {
        code.AppendLine($"    private static readonly global::System.Threading.AsyncLocal<{type}> __{name} = new();");
        code.AppendLine($"    public static {type} {name} {{ get => __{name}.Value; internal set => __{name}.Value = value; }}");
    }

    /// <summary>
    /// Parametres exposes par <see cref="GlobalsClass"/>, sous leur nom sans @. Un nom qui n'est pas un identifiant C#,
    /// un mot reserve ou un doublon n'est pas expose : le script ne peut pas le nommer.
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

    /// <summary>Valeur du parametre convertie en son type, null sans valeur. Une valeur qui ne convient pas au type leve <see cref="ArgumentException"/>.</summary>
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

    /// <summary>Usings implicites et alias du contexte, document fixe du projet ; la requete peut declarer les siens.</summary>
    internal static string BuildGlobalUsings(EfGeneratedModel? model)
    {
        var code = new StringBuilder();
        code.AppendLine("global using System;");
        code.AppendLine("global using System.Collections.Generic;");
        code.AppendLine("global using System.Linq;");
        code.AppendLine("global using System.Threading.Tasks;");
        code.AppendLine("global using Microsoft.EntityFrameworkCore;");
        code.AppendLine($"global using static {GlobalsClass};");

        // Sans connexion, l'alias designe DbContext : la requete reste analysable.
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
        // Assemblies du runtime (a defaut, celles deja chargees), filtrees.
        var paths = CSharpReferences.GetTrustedPlatformAssemblyPaths()
            .Where(path => ReferencedAssemblyPrefixes.Any(p => Path.GetFileName(path).StartsWith(p, StringComparison.OrdinalIgnoreCase)));

        // EF et le fournisseur ne sont peut-etre pas encore charges : on force leurs assemblies dans la liste.
        string[] required = [typeof(DbContext).Assembly.Location, typeof(RelationalQueryableExtensions).Assembly.Location, .. (provider?.Assemblies ?? []).Select(a => a.Location)];

        return paths.Concat(required)
                    .Where(path => !string.IsNullOrEmpty(path))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Select(path => (MetadataReference)MetadataReference.CreateFromFile(path))
                    .ToArray();
    }
}

