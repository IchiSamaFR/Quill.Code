using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Quill.Code.CSharp;

/// <summary>Roslyn project of a <see cref="CSharpLanguageService"/>.</summary>
public sealed record CSharpLanguageOptions
{
    /// <summary>Name of the project (and of its assembly).</summary>
    public string Name { get; init; } = "Program";

    /// <summary>Metadata references (see <see cref="CSharpReferences"/>).</summary>
    public IEnumerable<MetadataReference> References { get; init; } = [];

    /// <summary>Compilation options; must match those of the execution so that what is suggested compiles.</summary>
    public CSharpCompilationOptions CompilationOptions { get; init; } = new(OutputKind.ConsoleApplication, nullableContextOptions: NullableContextOptions.Enable);

    /// <summary>Parse options.</summary>
    public CSharpParseOptions ParseOptions { get; init; } = new(LanguageVersion.Latest);

    /// <summary>Fixed documents added to the project (name → source), e.g. <c>GlobalUsings.cs</c>.</summary>
    public IReadOnlyDictionary<string, string> AdditionalDocuments { get; init; } = new Dictionary<string, string>();

    /// <summary>Name of the document receiving the editor text.</summary>
    public string DocumentName { get; init; } = "Program.cs";

    /// <summary>
    /// Source analyzed instead of the editor text, of the same length to keep positions
    /// (e.g. script directives turned into comments); null: the text as is.
    /// </summary>
    public Func<string, string>? SourceTransform { get; init; }

    /// <summary>
    /// References specific to the text (e.g. packages declared by a script), added to <see cref="References"/>.
    /// The project is only recreated when they change: return reused instances
    /// (<see cref="CSharpReferences.FromFile"/>).
    /// </summary>
    public Func<string, IEnumerable<MetadataReference>>? TextReferences { get; init; }

    /// <summary>
    /// False to report syntax errors only, when the context is missing to resolve names
    /// (e.g. query without a DbContext).
    /// </summary>
    public bool IncludeSemanticDiagnostics { get; init; } = true;

    /// <summary>
    /// Adds the script runtime, generated in the assembly (no reference to the application): <c>Log</c> and
    /// <c>LogLevel</c>, wired through <see cref="Execution.RunOptions.Log"/>, and <c>Script.CancellationToken</c>,
    /// set by <see cref="Compilation.CSharpCompilationResult.RunAsync{T}"/>.
    /// </summary>
    public bool IncludeLogRuntime { get; init; } = true;
}
