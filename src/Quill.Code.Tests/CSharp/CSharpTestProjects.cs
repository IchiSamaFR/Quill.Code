using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Quill.Code.CSharp;

namespace Quill.Code.Tests.CSharp;

/// <summary>Services shared by the tests: framework references are expensive to read.</summary>
internal static class CSharpTestProjects
{
    public static IEnumerable<MetadataReference> References => LazyReferences.Value;

    private static readonly Lazy<IEnumerable<MetadataReference>> LazyReferences = new(() => CSharpReferences.FromTrustedPlatformAssemblies());

    /// <summary>Top-level statements, like a PocketForge script or a Querify LINQ query.</summary>
    public static CSharpLanguageService Script { get; } = new(() => ScriptOptions("Script"));

    /// <summary>Options of a script: console application, <c>System</c> imported.</summary>
    public static CSharpLanguageOptions ScriptOptions(string name) => new()
    {
        Name = name,
        References = References,
        CompilationOptions = new CSharpCompilationOptions(OutputKind.ConsoleApplication, nullableContextOptions: NullableContextOptions.Enable),
        AdditionalDocuments = new Dictionary<string, string> { ["GlobalUsings.cs"] = "global using System;" },
    };

    /// <summary>Options of a library.</summary>
    public static CSharpLanguageOptions LibraryOptions(string name) => new()
    {
        Name = name,
        References = References,
        CompilationOptions = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary),
    };
}
