using Quill.Code.Analysis;
using Quill.Code.Completion;
using Quill.Code.CSharp.Analysis;
using Quill.Code.CSharp.Compilation;
using Quill.Code.CSharp.Completion;
using Quill.Code.CSharp.Workspaces;
using Quill.Code.Languages;

namespace Quill.Code.CSharp;

/// <summary>
/// Services d'éditeur C# fournis par Roslyn, sur un projet propre au service, créé à la première demande (hors du
/// thread de l'interface). Les positions reçues et rendues sont celles du texte de l'éditeur
/// (<see cref="CSharpDocument.Text"/>), jamais celles du source généré. Chaque demande est relayée au service de la
/// fonctionnalité (<see cref="Completion"/>, <see cref="Diagnostics"/>, <see cref="Hover"/>, <see cref="Signature"/>),
/// qui accepte aussi un document de n'importe quel projet.
/// </summary>
/// <remarks>
/// Les documents sont des instantanés immuables : les demandes peuvent être concurrentes.
/// Roslyn calcule en partie de façon synchrone : chaque demande part sur le pool de threads pour ne pas figer l'interface.
/// </remarks>
public sealed class CSharpLanguageService : ICodeLanguageProvider, IDisposable
{
    private const string DefaultProjectName = "Document";

    private readonly CSharpWorkspace _workspace = new();

    /// <summary>Service sur un projet par défaut : bibliothèque C# référençant le framework.</summary>
    public CSharpLanguageService()
        : this(DefaultProjectName)
    {
    }

    /// <summary>Service sur son propre projet, créé à la première demande (hors du thread de l'interface).</summary>
    /// <param name="projectName">Nom du projet Roslyn (et de son assembly).</param>
    /// <param name="options">
    /// Options du projet, construites à la première demande ; à défaut, les références du framework.
    /// Leur <see cref="CSharpProjectOptions.Name"/> est remplacé par <paramref name="projectName"/>.
    /// </param>
    public CSharpLanguageService(string projectName, Func<CSharpProjectOptions>? options = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectName);

        Project = _workspace.AddProject(() => (options?.Invoke() ?? DefaultOptions()) with { Name = projectName });
        Completion = new CSharpCompletionService(Project);
        Diagnostics = new CSharpDiagnosticService(Project);
        Hover = new CSharpHoverService(Project);
        Signature = new CSharpSignatureService(Project);
    }

    /// <inheritdoc />
    public string Language => CodeLanguages.CSharp;

    /// <summary>Projet du service, créé à la première demande : utilisé par les méthodes qui prennent le code.</summary>
    public CSharpProject Project { get; }

    /// <summary>Autocomplétion.</summary>
    public CSharpCompletionService Completion { get; }

    /// <summary>Diagnostics.</summary>
    public CSharpDiagnosticService Diagnostics { get; }

    /// <summary>Infobulles.</summary>
    public CSharpHoverService Hover { get; }

    /// <summary>Aide à la signature.</summary>
    public CSharpSignatureService Signature { get; }

    /// <summary>Précharge Roslyn (composition MEF, métadonnées, JIT) : sinon la première demande prend une à deux secondes.</summary>
    /// <param name="code">Texte de préchauffage ; « Console. » prépare aussi la recherche des membres.</param>
    public Task WarmUpAsync(string code = "", CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(code);
        return Completion.GetCompletionsAsync(code, code.Length, cancellationToken);
    }

    /// <inheritdoc />
    public Task<CompletionList> GetCompletionsAsync(string code, int position, CancellationToken cancellationToken = default)
        => Completion.GetCompletionsAsync(code, position, cancellationToken);

    /// <inheritdoc />
    public Task<CompletionChange> GetChangeAsync(string code, CompletionItem item, CancellationToken cancellationToken = default)
        => Completion.GetChangeAsync(code, item, cancellationToken);

    /// <inheritdoc />
    public Task<IReadOnlyList<CodeDiagnostic>> GetDiagnosticsAsync(string code, CancellationToken cancellationToken = default)
        => Diagnostics.GetDiagnosticsAsync(code, cancellationToken);

    /// <inheritdoc />
    public Task<HoverInfo?> GetHoverAsync(string code, int position, CancellationToken cancellationToken = default)
        => Hover.GetHoverAsync(code, position, cancellationToken);

    /// <inheritdoc />
    public Task<SignatureHelpInfo?> GetSignatureHelpAsync(string code, int position, CancellationToken cancellationToken = default)
        => Signature.GetSignatureHelpAsync(code, position, cancellationToken);

    /// <summary>
    /// Compile <paramref name="code"/> dans le projet du service (mêmes références, options et sources que l'éditeur),
    /// puis charge l'assembly dans un contexte déchargeable. Les erreurs sont rendues, pas levées.
    /// </summary>
    public Task<CSharpCompilationResult> CompileAsync(string code, CSharpCompileOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(code);
        return Task.Run(() => CSharpCompiler.CompileAsync(Project.CreateDocument(code), options ?? new(), cancellationToken), cancellationToken);
    }

    /// <inheritdoc cref="CompileAsync(string, CSharpCompileOptions?, CancellationToken)"/>
    public Task<CSharpCompilationResult> CompileAsync(CSharpDocument document, CSharpCompileOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);
        return Task.Run(() => CSharpCompiler.CompileAsync(document, options ?? new(), cancellationToken), cancellationToken);
    }

    /// <inheritdoc />
    public void Dispose() => _workspace.Dispose();

    private static CSharpProjectOptions DefaultOptions() => new() { References = CSharpReferences.FromTrustedPlatformAssemblies() };
}
