using Microsoft.CodeAnalysis;
using Quill.Code.Analysis;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Runtime.Loader;

namespace Quill.Code.CSharp.Compilation;

/// <summary>
/// Code compiled and loaded into a collectible context, or the compilation errors; <see cref="RunAsync{T}"/>
/// runs it. <see cref="Dispose"/> unloads the assembly, when an abandoned execution actually ends.
/// </summary>
public sealed class CSharpCompilationResult : IDisposable
{
    private readonly AssemblyLoadContext? _loadContext;
    private readonly Lazy<MetadataReference>? _reference;
    private Task? _run;
    private int _isDisposed;

    internal CSharpCompilationResult(IReadOnlyList<CodeDiagnostic> diagnostics, Assembly? assembly, AssemblyLoadContext? loadContext, byte[]? image = null)
    {
        Diagnostics = diagnostics;
        Assembly = assembly;
        _loadContext = loadContext;
        _reference = image is null ? null : new(() => MetadataReference.CreateFromImage(image));
    }

    /// <summary>Errors and warnings, at editor text positions.</summary>
    public IReadOnlyList<CodeDiagnostic> Diagnostics { get; }

    /// <summary>Errors only.</summary>
    public IReadOnlyList<CodeDiagnostic> Errors => Diagnostics.Where(d => d.IsError).ToArray();

    /// <summary>Loaded assembly, null on compilation error.</summary>
    public Assembly? Assembly { get; }

    public bool Success => Assembly is not null;

    /// <summary>
    /// Roslyn reference to the emitted assembly, to compile another project against it
    /// (e.g. model referenced by the editor); null on compilation error.
    /// </summary>
    public MetadataReference? Reference => _reference?.Value;

    /// <summary>
    /// Runs <paramref name="run"/> on the assembly, off the calling thread, with <c>Script.CancellationToken</c>
    /// (script runtime) set to <paramref name="cancellationToken"/>. An exception from the compiled code is rethrown
    /// as is, without <see cref="TargetInvocationException"/>.
    /// </summary>
    /// <remarks>
    /// The code cannot be interrupted in-process: on cancellation, control returns immediately
    /// (<see cref="CSharpRunAbandonedException"/>) and it finishes in the background; <see cref="Dispose"/> then waits
    /// for it to actually end before unloading the assembly.
    /// </remarks>
    /// <exception cref="InvalidOperationException">Failed compilation: nothing to run.</exception>
    public Task<T> RunAsync<T>(Func<Assembly, T> run, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(run);
        var assembly = Assembly ?? throw new InvalidOperationException("Compilation failed: nothing to run.");
        cancellationToken.ThrowIfCancellationRequested();

        var task = Task.Run(() =>
        {
            CSharpScriptRuntime.SetCancellationToken(assembly, cancellationToken);
            try
            {
                return run(assembly);
            }
            catch (TargetInvocationException ex) when (ex.InnerException is { } inner)
            {
                ExceptionDispatchInfo.Capture(inner).Throw();
                throw;
            }
        }, CancellationToken.None);

        Volatile.Write(ref _run, task);
        return WaitAsync(task, cancellationToken);
    }

    /// <summary>
    /// Runs the entry point (<c>Main</c> or top-level statements, async included) like
    /// <see cref="RunAsync{T}"/>; returns its exit code, null if it has none.
    /// </summary>
    public async Task<int?> RunEntryPointAsync(string[]? args = null, CancellationToken cancellationToken = default)
    {
        var result = await RunAsync(assembly =>
        {
            var entryPoint = assembly.EntryPoint ?? throw new InvalidOperationException("The compiled code has no entry point.");
            return entryPoint.Invoke(null, entryPoint.GetParameters().Length == 0 ? null : new object?[] { args ?? Array.Empty<string>() });
        }, cancellationToken).ConfigureAwait(false);

        return result as int?;
    }

    /// <summary>
    /// The assembly is released by the GC as soon as nothing references its types anymore; no effect if it was loaded
    /// with <see cref="CSharpCompileOptions.Collectible"/> set to false. After an abandoned execution, unloading
    /// waits for it to actually end.
    /// </summary>
    public void Dispose()
    {
        if (Volatile.Read(ref _run) is { IsCompleted: false } run)
        {
            _ = run.ContinueWith(_ => Unload(), CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
            return;
        }

        Unload();
    }

    private static async Task<T> WaitAsync<T>(Task<T> run, CancellationToken cancellationToken)
    {
        try
        {
            return await run.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!run.IsCompleted)
        {
            // Possible exception observed here: nobody cares about the result anymore.
            _ = run.ContinueWith(static t => _ = t.Exception, CancellationToken.None, TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
            throw new CSharpRunAbandonedException(cancellationToken);
        }
    }

    private void Unload()
    {
        if (Interlocked.Exchange(ref _isDisposed, 1) == 0 && _loadContext is { IsCollectible: true })
            _loadContext.Unload();
    }
}
