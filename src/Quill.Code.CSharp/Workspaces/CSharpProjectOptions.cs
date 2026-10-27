using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Quill.Code.CSharp.Workspaces;

/// <summary>Projet Roslyn créé par <see cref="CSharpWorkspace.AddProject"/>.</summary>
public sealed class CSharpProjectOptions
{
    /// <summary>Nom du projet (et de son assembly).</summary>
    public required string Name { get; init; }

    /// <summary>Références de métadonnées (voir <see cref="CSharpReferences"/>).</summary>
    public IEnumerable<MetadataReference> References { get; init; } = [];

    /// <summary>Options de compilation ; doivent être celles de l'exécution pour que ce qui est proposé compile.</summary>
    public CSharpCompilationOptions CompilationOptions { get; init; } = new(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable);

    /// <summary>Options d'analyse syntaxique.</summary>
    public CSharpParseOptions ParseOptions { get; init; } = new(LanguageVersion.Latest);

    /// <summary>Documents fixes ajoutés au projet (nom → source), ex. <c>GlobalUsings.cs</c>.</summary>
    public IReadOnlyDictionary<string, string> AdditionalDocuments { get; init; } = new Dictionary<string, string>();

    /// <summary>Nom du document qui reçoit le texte de l'éditeur.</summary>
    public string DocumentName { get; init; } = "Document.cs";
}
