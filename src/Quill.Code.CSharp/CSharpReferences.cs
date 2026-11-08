using Microsoft.CodeAnalysis;
using System.Collections.Concurrent;
using System.Collections.Immutable;

namespace Quill.Code.CSharp;

/// <summary>Builds the metadata references of a project.</summary>
public static class CSharpReferences
{
    // One reference per file, reused: Roslyn does not recompile if the references are the same instances.
    private static readonly ConcurrentDictionary<string, MetadataReference> FileReferences = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>True for a .NET framework assembly (excludes the application's own, which would pollute completion).</summary>
    public static bool IsFrameworkAssembly(string fileName)
    {
        ArgumentNullException.ThrowIfNull(fileName);

        return fileName.StartsWith("System.", StringComparison.OrdinalIgnoreCase)
            || fileName.Equals("System.dll", StringComparison.OrdinalIgnoreCase)
            || fileName.Equals("mscorlib.dll", StringComparison.OrdinalIgnoreCase)
            || fileName.Equals("netstandard.dll", StringComparison.OrdinalIgnoreCase)
            || fileName.Equals("Microsoft.CSharp.dll", StringComparison.OrdinalIgnoreCase)
            || fileName.Equals("Microsoft.Win32.Primitives.dll", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Paths of the current runtime's assemblies (TRUSTED_PLATFORM_ASSEMBLIES, otherwise those already loaded).</summary>
    public static IReadOnlyList<string> GetTrustedPlatformAssemblyPaths()
        => (AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string)?.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
            ?? AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
                .Select(a => a.Location)
                .ToArray();

    /// <summary>
    /// References of the runtime assemblies selected by <paramref name="include"/> (file name),
    /// <see cref="IsFrameworkAssembly"/> by default. Expensive: the caller should cache them.
    /// </summary>
    public static ImmutableArray<MetadataReference> FromTrustedPlatformAssemblies(Func<string, bool>? include = null)
    {
        include ??= IsFrameworkAssembly;

        return GetTrustedPlatformAssemblyPaths()
            .Where(path => include(Path.GetFileName(path)))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(path => (MetadataReference)MetadataReference.CreateFromFile(path))
            .ToImmutableArray();
    }

    /// <summary>
    /// Reference to an assembly read into memory, cached by path. A mapped file would stay locked
    /// while the application runs (impossible to delete a package from the cache).
    /// </summary>
    public static MetadataReference FromFile(string path)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);

        return FileReferences.GetOrAdd(path, p => AssemblyMetadata.CreateFromImage(File.ReadAllBytes(p)).GetReference(filePath: p));
    }
}
