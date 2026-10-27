using Microsoft.CodeAnalysis;
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
    private readonly Lazy<MetadataReference>? _reference;
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
    /// L'assembly est libérée par le GC dès que plus rien ne référence ses types ; sans effet si elle a été chargée
    /// avec <see cref="CSharpCompileOptions.Collectible"/> à faux.
    /// </summary>
    public void Dispose()
    {
        if (Interlocked.Exchange(ref _isDisposed, 1) == 0 && _loadContext is { IsCollectible: true })
            _loadContext.Unload();
    }
}
