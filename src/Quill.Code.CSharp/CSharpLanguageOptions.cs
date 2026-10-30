using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Quill.Code.CSharp;

/// <summary>Projet Roslyn d'un <see cref="CSharpLanguageService"/>.</summary>
public sealed record CSharpLanguageOptions
{
    /// <summary>Nom du projet (et de son assembly).</summary>
    public string Name { get; init; } = "Program";

    /// <summary>Références de métadonnées (voir <see cref="CSharpReferences"/>).</summary>
    public IEnumerable<MetadataReference> References { get; init; } = [];

    /// <summary>Options de compilation ; doivent être celles de l'exécution pour que ce qui est proposé compile.</summary>
    public CSharpCompilationOptions CompilationOptions { get; init; } = new(OutputKind.ConsoleApplication, nullableContextOptions: NullableContextOptions.Enable);

    /// <summary>Options d'analyse syntaxique.</summary>
    public CSharpParseOptions ParseOptions { get; init; } = new(LanguageVersion.Latest);

    /// <summary>Documents fixes ajoutés au projet (nom → source), ex. <c>GlobalUsings.cs</c>.</summary>
    public IReadOnlyDictionary<string, string> AdditionalDocuments { get; init; } = new Dictionary<string, string>();

    /// <summary>Nom du document qui reçoit le texte de l'éditeur.</summary>
    public string DocumentName { get; init; } = "Program.cs";

    /// <summary>
    /// Source analysé à la place du texte de l'éditeur, de même longueur pour garder les positions
    /// (ex. directives d'un script neutralisées en commentaires) ; null : le texte tel quel.
    /// </summary>
    public Func<string, string>? SourceTransform { get; init; }

    /// <summary>
    /// Références propres au texte (ex. packages déclarés par un script), ajoutées à <see cref="References"/>.
    /// Le projet n'est recréé que lorsqu'elles changent : à renvoyer sous forme d'instances réutilisées
    /// (<see cref="CSharpReferences.FromFile"/>).
    /// </summary>
    public Func<string, IEnumerable<MetadataReference>>? TextReferences { get; init; }

    /// <summary>
    /// Faux pour ne signaler que les fautes de syntaxe, quand le contexte manque pour résoudre les noms
    /// (ex. requête sans DbContext).
    /// </summary>
    public bool IncludeSemanticDiagnostics { get; init; } = true;

    /// <summary>
    /// Ajoute le runtime des scripts, généré dans l'assembly (aucune référence vers l'application) : <c>Log</c> et
    /// <c>LogLevel</c>, branchés par <see cref="Execution.RunOptions.Log"/>, et <c>Script.CancellationToken</c>,
    /// fixé par <see cref="Compilation.CSharpCompilationResult.RunAsync{T}"/>.
    /// </summary>
    public bool IncludeLogRuntime { get; init; } = true;
}
