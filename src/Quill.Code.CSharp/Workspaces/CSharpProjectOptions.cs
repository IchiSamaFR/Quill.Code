using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Quill.Code.CSharp.Workspaces;

/// <summary>Projet Roslyn créé par <see cref="CSharpWorkspace.AddProject"/>.</summary>
public sealed record CSharpProjectOptions
{
    /// <summary>Nom du projet (et de son assembly).</summary>
    public string Name { get; init; } = "Document";

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

    /// <summary>
    /// Source analysé pour le texte de l'éditeur : le texte neutralisé à longueur égale (ex. directives d'un script,
    /// une chaîne suffit), ou placé dans du code généré à une position donnée (ex. requête dans une méthode) ;
    /// null : le texte tel quel. Les positions échangées restent celles du texte de l'éditeur.
    /// </summary>
    public Func<string, CSharpSource>? SourceTransform { get; init; }

    /// <summary>
    /// Faux pour ne signaler que les fautes de syntaxe, quand le contexte manque pour résoudre les noms
    /// (ex. requête sans DbContext).
    /// </summary>
    public bool IncludeSemanticDiagnostics { get; init; } = true;

    /// <summary>
    /// Faux quand le texte ne peut pas recevoir de « using » : un type d'un espace de noms non importé est alors
    /// inséré avec son nom complet, et une méthode d'extension non importée n'est pas proposée.
    /// </summary>
    public bool AllowImports { get; init; } = true;

    /// <summary>
    /// Références propres au texte (ex. packages déclarés par un script), ajoutées à <see cref="References"/>.
    /// Le projet n'est recréé que lorsqu'elles changent : à renvoyer sous forme d'instances réutilisées
    /// (<see cref="CSharpReferences.FromFile"/>).
    /// </summary>
    public Func<string, IEnumerable<MetadataReference>>? TextReferences { get; init; }
}
