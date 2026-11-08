using Quill.Code.Execution;
using System.Reflection;

namespace Quill.Code.CSharp.Compilation;

/// <summary>
/// Settings for <see cref="CSharpLanguageService.CompileAsync(string, CSharpCompileOptions?, CancellationToken)"/> and
/// <see cref="CSharpLanguageService.RunAsync"/>; <see cref="RunOptions.Log"/> receives the <c>Log</c> messages of the compiled
/// code (<see cref="Scripting.LogSource.Code"/>), specific to this compilation, and requires <see cref="CSharpLanguageOptions.IncludeLogRuntime"/>.
/// </summary>
public sealed record CSharpCompileOptions : RunOptions
{
    /// <summary>Name of the emitted assembly; by default the project name followed by a unique identifier.</summary>
    public string? AssemblyName { get; init; }

    /// <summary>
    /// Assemblies (files) the code can load, resolved by name and read into memory so as not to lock
    /// the files (e.g. a script's packages).
    /// </summary>
    public IEnumerable<string> AssemblyFiles { get; init; } = [];

    /// <summary>
    /// Resolution of other assemblies requested by the code, before <see cref="AssemblyFiles"/> (e.g. model already
    /// loaded in another context); null: the runtime provides them.
    /// </summary>
    public Func<AssemblyName, Assembly?>? ResolveAssembly { get; init; }

    /// <summary>
    /// False to load the assembly for the application's whole lifetime (e.g. model whose types a library
    /// caches): <see cref="CSharpCompilationResult.Dispose"/> then does not unload it.
    /// </summary>
    public bool Collectible { get; init; } = true;
}
