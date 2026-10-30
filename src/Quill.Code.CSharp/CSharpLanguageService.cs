using Quill.Code.Analysis;
using Quill.Code.Completion;
using Quill.Code.CSharp.Compilation;
using Quill.Code.CSharp.Features;
using Quill.Code.CSharp.Roslyn;
using Quill.Code.Constants;

namespace Quill.Code.CSharp;

/// <summary>
/// Services d'éditeur C# fournis par Roslyn (autocomplétion, diagnostics, survol, signatures) et compilation, sur un
/// projet propre au service, créé à la première demande. Les positions reçues et rendues sont celles du texte de l'éditeur.
/// </summary>
/// <remarks>
/// Chaque demande travaille sur un instantané : les demandes peuvent être concurrentes. Roslyn calcule en partie de
/// façon synchrone : chaque demande part sur le pool de threads pour ne pas figer l'interface.
/// </remarks>
public sealed class CSharpLanguageService : ICodeLanguageService
{
    private readonly CSharpProject _project;

    /// <summary>Service sur son propre projet, créé à la première demande (hors du thread de l'interface).</summary>
    /// <param name="options">
    /// Options du projet, construites à la première demande (ex. références lues sur le disque) ;
    /// à défaut, une bibliothèque référençant le framework.
    /// </param>
    public CSharpLanguageService(Func<CSharpLanguageOptions>? options = null)
    {
        _project = new CSharpProject(options ?? DefaultOptions);
        _project.SourcesChanged += (_, e) => SourcesChanged?.Invoke(this, e);
    }

    /// <summary>Levé quand une source est ajoutée, remplacée ou retirée (ex. pour relancer l'analyse de l'éditeur).</summary>
    public event EventHandler? SourcesChanged;

    /// <inheritdoc />
    public string Language => CodeLanguages.CSharp;

    /// <summary>Clés des sources ajoutées à la volée.</summary>
    public IReadOnlyCollection<string> Sources => _project.Sources;

    internal CSharpProject Project => _project;

    /// <summary>Ajoute ou remplace une source (ex. une classe utilitaire), vue par l'éditeur et compilée avec lui.</summary>
    /// <param name="name">Nom du document (ex. <c>FileHelper.cs</c>), repris dans les erreurs de compilation.</param>
    /// <param name="code">Code C# de la source.</param>
    public void SetSource(string name, string code) => _project.SetSource(name, code);

    /// <summary>Lit un fichier .cs et l'ajoute (ou le remplace) ; il est identifié par son chemin complet.</summary>
    /// <returns>Clé de la source, à passer à <see cref="RemoveSource"/>.</returns>
    public Task<string> SetSourceFileAsync(string path, CancellationToken cancellationToken = default)
        => _project.SetSourceFileAsync(path, cancellationToken);

    /// <summary>Retire une source ajoutée à la volée (nom ou chemin complet).</summary>
    /// <returns>Faux si aucune source ne porte cette clé.</returns>
    public bool RemoveSource(string key) => _project.RemoveSource(key);

    /// <summary>Retire toutes les sources ajoutées à la volée.</summary>
    public void ClearSources() => _project.ClearSources();

    /// <summary>Précharge Roslyn (composition MEF, métadonnées, JIT) : sinon la première demande prend une à deux secondes.</summary>
    /// <param name="code">Texte de préchauffage ; « Console. » prépare aussi la recherche des membres.</param>
    public Task WarmUpAsync(string code = "", CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(code);
        return GetCompletionsAsync(code, code.Length, cancellationToken);
    }

    /// <inheritdoc />
    public Task<CompletionList> GetCompletionsAsync(string code, int position, CancellationToken cancellationToken = default)
        => RunAsync(code, (document, token) => CSharpCompletionEngine.GetCompletionsAsync(document, position, token), cancellationToken);

    /// <inheritdoc />
    public Task<CompletionChange> GetChangeAsync(string code, CompletionItem item, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(item);
        return RunAsync(code, (document, token) => CSharpCompletionEngine.GetChangeAsync(document, item, token), cancellationToken);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<CodeDiagnostic>> GetDiagnosticsAsync(string code, CancellationToken cancellationToken = default)
        => RunAsync(code, CSharpDiagnosticEngine.GetDiagnosticsAsync, cancellationToken);

    /// <inheritdoc />
    public Task<HoverInfo?> GetHoverAsync(string code, int position, CancellationToken cancellationToken = default)
        => RunAsync(code, (document, token) => CSharpHoverEngine.GetHoverAsync(document, position, token), cancellationToken);

    /// <inheritdoc />
    public Task<SignatureHelpInfo?> GetSignatureHelpAsync(string code, int position, CancellationToken cancellationToken = default)
        => RunAsync(code, (document, token) => CSharpSignatureHelpEngine.GetSignatureHelpAsync(document, position, token), cancellationToken);

    /// <summary>
    /// Compile <paramref name="code"/> dans le projet du service (mêmes références, options et sources que l'éditeur),
    /// puis charge l'assembly dans un contexte déchargeable. Les erreurs sont rendues, pas levées.
    /// </summary>
    public Task<CSharpCompilationResult> CompileAsync(string code, CSharpCompileOptions? options = null, CancellationToken cancellationToken = default)
        => RunAsync(code, (document, token) => CSharpCompiler.CompileAsync(document, options ?? new(), token), cancellationToken);

    // Document créé sur le pool lui aussi : la première demande construit le projet (MEF, références).
    private Task<T> RunAsync<T>(string code, Func<CSharpDocument, CancellationToken, Task<T>> request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(code);
        return Task.Run(() => request(_project.CreateDocument(code), cancellationToken), cancellationToken);
    }

    private static CSharpLanguageOptions DefaultOptions() => new() { References = CSharpReferences.FromTrustedPlatformAssemblies() };
}
