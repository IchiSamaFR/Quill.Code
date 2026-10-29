using Quill.Code.Analysis;
using Quill.Code.Completion;
using Quill.Code.CSharp.Analysis;
using Quill.Code.CSharp.Compilation;
using Quill.Code.CSharp.Completion;
using Quill.Code.CSharp.Workspaces;
using Quill.Code.Languages;

namespace Quill.Code.CSharp;

/// <inheritdoc cref="ICSharpLanguageService"/>
/// <remarks>
/// Les documents sont des instantanés immuables : les demandes peuvent être concurrentes.
/// Roslyn calcule en partie de façon synchrone : chaque demande part sur le pool de threads pour ne pas figer l'interface.
/// </remarks>
public sealed class CSharpLanguageService : ICSharpLanguageService
{
    private const string DefaultProjectName = "Document";

    /// <inheritdoc />
    public string Language => CodeLanguages.CSharp;

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
    }

    /// <inheritdoc />
    public CSharpProject Project { get; }

    /// <inheritdoc />
    public Task WarmUpAsync(string code = "", CancellationToken cancellationToken = default)
        => this.WarmUpAsync(Project, code, cancellationToken);

    /// <inheritdoc />
    public Task<CompletionList> GetCompletionsAsync(string code, int position, CancellationToken cancellationToken = default)
        => this.GetCompletionsAsync(Project, code, position, cancellationToken);

    /// <inheritdoc />
    public Task<CompletionChange> GetChangeAsync(string code, CompletionItem item, CancellationToken cancellationToken = default)
        => this.GetChangeAsync(Project, code, item, cancellationToken);

    /// <inheritdoc />
    public Task<IReadOnlyList<CodeDiagnostic>> GetDiagnosticsAsync(string code, CancellationToken cancellationToken = default)
        => this.GetDiagnosticsAsync(Project, code, cancellationToken);

    /// <inheritdoc />
    public Task<CSharpCompilationResult> CompileAsync(string code, CSharpCompileOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(code);
        return Task.Run(() => CSharpCompiler.CompileAsync(Project.CreateDocument(code), options ?? new(), cancellationToken), cancellationToken);
    }

    /// <inheritdoc />
    public Task<CSharpCompilationResult> CompileAsync(CSharpDocument document, CSharpCompileOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);
        return Task.Run(() => CSharpCompiler.CompileAsync(document, options ?? new(), cancellationToken), cancellationToken);
    }

    /// <inheritdoc />
    public Task<HoverInfo?> GetHoverAsync(string code, int position, CancellationToken cancellationToken = default)
        => this.GetHoverAsync(Project, code, position, cancellationToken);

    /// <inheritdoc />
    public Task<SignatureHelpInfo?> GetSignatureHelpAsync(string code, int position, CancellationToken cancellationToken = default)
        => this.GetSignatureHelpAsync(Project, code, position, cancellationToken);

    /// <inheritdoc />
    public void Dispose() => _workspace.Dispose();

    private static CSharpProjectOptions DefaultOptions() => new() { References = CSharpReferences.FromTrustedPlatformAssemblies() };

    /// <inheritdoc />
    public Task WarmUpAsync(CSharpDocument document, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);
        return GetCompletionsAsync(document, document.Text.Length, cancellationToken);
    }

    /// <inheritdoc />
    public Task<CompletionList> GetCompletionsAsync(CSharpDocument document, int position, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);
        return Task.Run(() => CSharpCompletionEngine.GetCompletionsAsync(document, position, cancellationToken), cancellationToken);
    }

    /// <inheritdoc />
    public Task<CompletionChange> GetChangeAsync(CSharpDocument document, CompletionItem item, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(item);
        return Task.Run(() => CSharpCompletionEngine.GetChangeAsync(document, item, cancellationToken), cancellationToken);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<CodeDiagnostic>> GetDiagnosticsAsync(CSharpDocument document, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);
        return Task.Run(() => CSharpDiagnosticEngine.GetDiagnosticsAsync(document, cancellationToken), cancellationToken);
    }

    /// <inheritdoc />
    public Task<HoverInfo?> GetHoverAsync(CSharpDocument document, int position, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);
        return Task.Run(() => CSharpHoverEngine.GetHoverAsync(document, position, cancellationToken), cancellationToken);
    }

    /// <inheritdoc />
    public Task<SignatureHelpInfo?> GetSignatureHelpAsync(CSharpDocument document, int position, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);
        return Task.Run(() => CSharpSignatureHelpEngine.GetSignatureHelpAsync(document, position, cancellationToken), cancellationToken);
    }
}
