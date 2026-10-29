using System.Reflection;
using System.Runtime.Loader;

namespace Quill.Code.CSharp.Compilation;

/// <summary>
/// Contexte du code compilé, déchargeable par défaut : chaque exécution ne laisse pas son assembly en mémoire. Les assemblies
/// demandées viennent de <see cref="CSharpCompileOptions.ResolveAssembly"/>, puis des fichiers fournis, puis du runtime.
/// </summary>
internal sealed class CompiledCodeLoadContext : AssemblyLoadContext
{
    private readonly Func<AssemblyName, Assembly?>? _resolve;
    private readonly Dictionary<string, string> _files;

    public CompiledCodeLoadContext(string name, CSharpCompileOptions options)
        : base(name, options.Collectible)
    {
        _resolve = options.ResolveAssembly;
        _files = options.AssemblyFiles
            .GroupBy(path => Path.GetFileNameWithoutExtension(path), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key!, g => g.First(), StringComparer.OrdinalIgnoreCase);
    }

    /// <inheritdoc cref="AssemblyLoadContext.Load(AssemblyName)"/>
    protected override Assembly? Load(AssemblyName assemblyName)
    {
        if (_resolve?.Invoke(assemblyName) is { } resolved)
            return resolved;

        // Lue en mémoire : un fichier chargé depuis le disque resterait verrouillé.
        return assemblyName.Name is { } name && _files.TryGetValue(name, out var path)
            ? LoadFromStream(new MemoryStream(File.ReadAllBytes(path)))
            : null;
    }
}
