using Quill.Code.Analysis;
using System.Reflection;
using System.Runtime.Loader;

namespace Quill.Code.CSharp.Compilation;

/// <summary>
/// Code compilé et chargé dans un contexte déchargeable, ou les erreurs de compilation.
/// <see cref="Dispose"/> décharge l'assembly : à appeler une fois l'exécution terminée.
/// </summary>
public sealed class CSharpCompilationResult : IDisposable
{
    private readonly AssemblyLoadContext? _loadContext;
    private int _isDisposed;

    internal CSharpCompilationResult(IReadOnlyList<CodeDiagnostic> diagnostics, Assembly? assembly, AssemblyLoadContext? loadContext)
    {
        Diagnostics = diagnostics;
        Assembly = assembly;
        _loadContext = loadContext;
    }

    /// <summary>Erreurs et avertissements, aux positions du texte de l'éditeur.</summary>
    public IReadOnlyList<CodeDiagnostic> Diagnostics { get; }

    /// <summary>Erreurs seules.</summary>
    public IReadOnlyList<CodeDiagnostic> Errors => Diagnostics.Where(d => d.IsError).ToArray();

    /// <summary>Assembly chargée, null en cas d'erreur de compilation.</summary>
    public Assembly? Assembly { get; }

    public bool Success => Assembly is not null;

    /// <summary>L'assembly est libérée par le GC dès que plus rien ne référence ses types.</summary>
    public void Dispose()
    {
        if (Interlocked.Exchange(ref _isDisposed, 1) == 0)
            _loadContext?.Unload();
    }
}
