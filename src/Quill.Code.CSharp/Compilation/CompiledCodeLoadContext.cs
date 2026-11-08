using System.Reflection;
using System.Runtime.Loader;

namespace Quill.Code.CSharp.Compilation;

/// <summary>
/// Context of the compiled code, collectible by default: each execution does not leave its assembly in memory. Requested
/// assemblies come from <see cref="CSharpCompileOptions.ResolveAssembly"/>, then from the provided files, then from the runtime.
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

        // Read into memory: a file loaded from disk would stay locked.
        return assemblyName.Name is { } name && _files.TryGetValue(name, out var path)
            ? LoadFromStream(new MemoryStream(File.ReadAllBytes(path)))
            : null;
    }
}
