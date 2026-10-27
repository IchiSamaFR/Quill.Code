using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Quill.Code.CSharp.Workspaces;

namespace Quill.Code.Tests.CSharp;

/// <summary>Projets partagés par les tests : la composition MEF de Roslyn est coûteuse.</summary>
internal static class CSharpTestProjects
{
    public const string WrapperPrefix = "class Wrapper { void Run() { ";
    public const string WrapperSuffix = " } }";

    private static readonly CSharpWorkspace Workspace = new();

    private static readonly Lazy<IEnumerable<MetadataReference>> References = new(() => CSharpReferences.FromTrustedPlatformAssemblies());

    /// <summary>Instructions de niveau supérieur, comme un script PocketForge.</summary>
    public static CSharpProject Script { get; } = Workspace.AddProject(new CSharpProjectOptions
    {
        Name = "Script",
        References = References.Value,
        CompilationOptions = new CSharpCompilationOptions(OutputKind.ConsoleApplication, nullableContextOptions: NullableContextOptions.Enable),
        AdditionalDocuments = new Dictionary<string, string> { ["GlobalUsings.cs"] = "global using System;" },
    });

    /// <summary>Texte enveloppé dans une méthode, comme une requête LINQ de Querify.</summary>
    public static CSharpProject Wrapped { get; } = Workspace.AddProject(new CSharpProjectOptions
    {
        Name = "Wrapped",
        References = References.Value,
    });

    public static CSharpDocument Wrap(string text)
        => Wrapped.CreateDocument(WrapperPrefix + text + WrapperSuffix, text, WrapperPrefix.Length);
}
