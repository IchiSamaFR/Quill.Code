using Microsoft.CodeAnalysis;
using Quill.Code.Analysis;
using Quill.Code.CSharp.Scripting;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Runtime.Loader;

namespace Quill.Code.CSharp.Compilation;

/// <summary>
/// Code compilé et chargé dans un contexte déchargeable, ou les erreurs de compilation ; <see cref="RunAsync{T}"/>
/// l'exécute. <see cref="Dispose"/> décharge l'assembly, à la fin réelle d'une exécution abandonnée.
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

    /// <summary>Erreurs et avertissements, aux positions du texte de l'éditeur.</summary>
    public IReadOnlyList<CodeDiagnostic> Diagnostics { get; }

    /// <summary>Erreurs seules.</summary>
    public IReadOnlyList<CodeDiagnostic> Errors => Diagnostics.Where(d => d.IsError).ToArray();

    /// <summary>Assembly chargée, null en cas d'erreur de compilation.</summary>
    public Assembly? Assembly { get; }

    public bool Success => Assembly is not null;

    /// <summary>
    /// Référence Roslyn vers l'assembly émise, pour compiler un autre projet contre elle
    /// (ex. modèle référencé par l'éditeur) ; null en cas d'erreur de compilation.
    /// </summary>
    public MetadataReference? Reference => _reference?.Value;

    /// <summary>
    /// Exécute <paramref name="run"/> sur l'assembly, hors du thread appelant, avec <c>Script.CancellationToken</c>
    /// (runtime des scripts) fixé à <paramref name="cancellationToken"/>. Une exception du code compilé est relevée
    /// telle quelle, sans <see cref="TargetInvocationException"/>.
    /// </summary>
    /// <remarks>
    /// Le code ne peut pas être interrompu dans le processus : à l'annulation, la main est rendue tout de suite
    /// (<see cref="CSharpRunAbandonedException"/>) et il se termine en arrière-plan ; <see cref="Dispose"/> attend
    /// alors sa fin réelle pour décharger l'assembly.
    /// </remarks>
    /// <exception cref="InvalidOperationException">Compilation en échec : rien à exécuter.</exception>
    public Task<T> RunAsync<T>(Func<Assembly, T> run, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(run);
        var assembly = Assembly ?? throw new InvalidOperationException("La compilation a échoué : rien à exécuter.");
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
    /// Exécute le point d'entrée (<c>Main</c> ou instructions de niveau supérieur, async compris) comme
    /// <see cref="RunAsync{T}"/> ; rend son code de sortie, null s'il n'en a pas.
    /// </summary>
    public async Task<int?> RunEntryPointAsync(string[]? args = null, CancellationToken cancellationToken = default)
    {
        var result = await RunAsync(assembly =>
        {
            var entryPoint = assembly.EntryPoint ?? throw new InvalidOperationException("Le code compilé n'a pas de point d'entrée.");
            return entryPoint.Invoke(null, entryPoint.GetParameters().Length == 0 ? null : new object?[] { args ?? Array.Empty<string>() });
        }, cancellationToken).ConfigureAwait(false);

        return result as int?;
    }

    /// <summary>
    /// L'assembly est libérée par le GC dès que plus rien ne référence ses types ; sans effet si elle a été chargée
    /// avec <see cref="CSharpCompileOptions.Collectible"/> à faux. Après une exécution abandonnée, le déchargement
    /// attend sa fin réelle.
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
            // Exception éventuelle observée ici : le résultat n'intéresse plus personne.
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
