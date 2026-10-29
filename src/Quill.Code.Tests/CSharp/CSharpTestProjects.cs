using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Quill.Code.CSharp;

namespace Quill.Code.Tests.CSharp;

/// <summary>Services partagés par les tests : les références du framework sont coûteuses à lire.</summary>
internal static class CSharpTestProjects
{
    public static IEnumerable<MetadataReference> References => LazyReferences.Value;

    private static readonly Lazy<IEnumerable<MetadataReference>> LazyReferences = new(() => CSharpReferences.FromTrustedPlatformAssemblies());

    /// <summary>Instructions de niveau supérieur, comme un script PocketForge ou une requête LINQ de Querify.</summary>
    public static CSharpLanguageService Script { get; } = new(() => ScriptOptions("Script"));

    /// <summary>Options d'un script : application console, <c>System</c> importé.</summary>
    public static CSharpLanguageOptions ScriptOptions(string name) => new()
    {
        Name = name,
        References = References,
        CompilationOptions = new CSharpCompilationOptions(OutputKind.ConsoleApplication, nullableContextOptions: NullableContextOptions.Enable),
        AdditionalDocuments = new Dictionary<string, string> { ["GlobalUsings.cs"] = "global using System;" },
    };

    /// <summary>Options d'une bibliothèque.</summary>
    public static CSharpLanguageOptions LibraryOptions(string name) => new()
    {
        Name = name,
        References = References,
        CompilationOptions = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary),
    };
}
