using Quill.Code.Analysis;
using Quill.Code.Completion;
using Quill.Code.CSharp.Compilation;
using Quill.Code.CSharp.Features;
using Quill.Code.CSharp.Roslyn;
using Quill.Code.Constants;

namespace Quill.Code.CSharp;

/// <summary>
/// C# editor services provided by Roslyn (completion, diagnostics, hover, signatures) and compilation, on a
/// project owned by the service, created on first request. Positions received and returned are those of the editor text.
/// </summary>
/// <remarks>
/// Each request works on a snapshot: requests may run concurrently. Roslyn computes partly
/// synchronously: each request runs on the thread pool so as not to freeze the UI.
/// </remarks>
public sealed class CSharpLanguageService : ICodeLanguageService
{
    private readonly CSharpProject _project;

    /// <summary>Service on its own project, created on first request (off the UI thread).</summary>
    /// <param name="options">
    /// Project options, built on first request (e.g. references read from disk);
    /// otherwise, a library referencing the framework.
    /// </param>
    public CSharpLanguageService(Func<CSharpLanguageOptions>? options = null)
    {
        _project = new CSharpProject(options ?? DefaultOptions);
        _project.SourcesChanged += (_, e) => SourcesChanged?.Invoke(this, e);
    }

    /// <summary>Raised when a source is added, replaced or removed (e.g. to rerun the editor analysis).</summary>
    public event EventHandler? SourcesChanged;

    /// <inheritdoc />
    public string Language => CodeLanguages.CSharp;

    /// <summary>Keys of the sources added on the fly.</summary>
    public IReadOnlyCollection<string> Sources => _project.Sources;

    internal CSharpProject Project => _project;

    /// <summary>Adds or replaces a source (e.g. a helper class), seen by the editor and compiled with it.</summary>
    /// <param name="name">Document name (e.g. <c>FileHelper.cs</c>), shown in compilation errors.</param>
    /// <param name="code">C# code of the source.</param>
    public void SetSource(string name, string code) => _project.SetSource(name, code);

    /// <summary>Reads a .cs file and adds (or replaces) it; it is identified by its full path.</summary>
    /// <returns>Key of the source, to pass to <see cref="RemoveSource"/>.</returns>
    public Task<string> SetSourceFileAsync(string path, CancellationToken cancellationToken = default)
        => _project.SetSourceFileAsync(path, cancellationToken);

    /// <summary>Removes a source added on the fly (name or full path).</summary>
    /// <returns>False if no source has this key.</returns>
    public bool RemoveSource(string key) => _project.RemoveSource(key);

    /// <summary>Removes all sources added on the fly.</summary>
    public void ClearSources() => _project.ClearSources();

    /// <summary>Preloads Roslyn (MEF composition, metadata, JIT): otherwise the first request takes one to two seconds.</summary>
    /// <param name="code">Warm-up text; "Console." also prepares member lookup.</param>
    public Task WarmUpAsync(string code = "", CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(code);
        return GetCompletionsAsync(code, code.Length, cancellationToken);
    }

    /// <inheritdoc />
    public Task<CompletionList> GetCompletionsAsync(string code, int position, CancellationToken cancellationToken = default)
        => OnThreadPoolAsync(code, (document, token) => CSharpCompletionEngine.GetCompletionsAsync(document, position, token), cancellationToken);

    /// <inheritdoc />
    public Task<CompletionChange> GetChangeAsync(string code, CompletionItem item, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(item);
        return OnThreadPoolAsync(code, (document, token) => CSharpCompletionEngine.GetChangeAsync(document, item, token), cancellationToken);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<CodeDiagnostic>> GetDiagnosticsAsync(string code, CancellationToken cancellationToken = default)
        => OnThreadPoolAsync(code, CSharpDiagnosticEngine.GetDiagnosticsAsync, cancellationToken);

    /// <inheritdoc />
    public Task<HoverInfo?> GetHoverAsync(string code, int position, CancellationToken cancellationToken = default)
        => OnThreadPoolAsync(code, (document, token) => CSharpHoverEngine.GetHoverAsync(document, position, token), cancellationToken);

    /// <inheritdoc />
    public Task<SignatureHelpInfo?> GetSignatureHelpAsync(string code, int position, CancellationToken cancellationToken = default)
        => OnThreadPoolAsync(code, (document, token) => CSharpSignatureHelpEngine.GetSignatureHelpAsync(document, position, token), cancellationToken);

    /// <summary>
    /// Compiles <paramref name="code"/> in the service's project (same references, options and sources as the editor),
    /// then loads the assembly into a collectible context. Errors are returned, not thrown.
    /// </summary>
    public Task<CSharpCompilationResult> CompileAsync(string code, CSharpCompileOptions? options = null, CancellationToken cancellationToken = default)
        => OnThreadPoolAsync(code, (document, token) => CSharpCompiler.CompileAsync(document, options ?? new(), token), cancellationToken);

    /// <summary>
    /// Compiles <paramref name="code"/> like <see cref="CompileAsync"/>, runs its entry point (<c>Main</c> or
    /// top-level statements, async included), then unloads the assembly. Nothing is returned: the code's output
    /// goes through <see cref="Execution.RunOptions.Log"/>, an exception from the code is rethrown as is.
    /// </summary>
    /// <remarks>
    /// The code cannot be interrupted in-process: on cancellation, control returns immediately
    /// (<see cref="CSharpRunAbandonedException"/>) and the assembly is only unloaded when the code actually ends.
    /// </remarks>
    /// <exception cref="CSharpCompilationException">The code does not compile; its diagnostics are in the exception.</exception>
    public async Task RunAsync(string code, CSharpCompileOptions? options = null, CancellationToken cancellationToken = default)
    {
        using var compiled = await CompileAsync(code, options, cancellationToken).ConfigureAwait(false);
        if (!compiled.Success)
            throw new CSharpCompilationException(compiled.Diagnostics);

        await compiled.RunEntryPointAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    // Document also created on the pool: the first request builds the project (MEF, references).
    private Task<T> OnThreadPoolAsync<T>(string code, Func<CSharpDocument, CancellationToken, Task<T>> request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(code);
        return Task.Run(() => request(_project.CreateDocument(code), cancellationToken), cancellationToken);
    }

    private static CSharpLanguageOptions DefaultOptions() => new() { References = CSharpReferences.FromTrustedPlatformAssemblies() };
}
