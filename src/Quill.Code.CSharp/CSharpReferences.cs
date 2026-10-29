using Microsoft.CodeAnalysis;
using System.Collections.Concurrent;
using System.Collections.Immutable;

namespace Quill.Code.CSharp;

/// <summary>Construction des références de métadonnées d'un projet.</summary>
public static class CSharpReferences
{
    // Une référence par fichier, réutilisée : Roslyn ne recompile pas si les références sont les mêmes instances.
    private static readonly ConcurrentDictionary<string, MetadataReference> FileReferences = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Vrai pour une assembly du framework .NET (exclut celles de l'application, qui pollueraient la complétion).</summary>
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

    /// <summary>Chemins des assemblies du runtime courant (TRUSTED_PLATFORM_ASSEMBLIES, à défaut celles déjà chargées).</summary>
    public static IReadOnlyList<string> GetTrustedPlatformAssemblyPaths()
        => (AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string)?.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
            ?? AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
                .Select(a => a.Location)
                .ToArray();

    /// <summary>
    /// Références des assemblies du runtime retenues par <paramref name="include"/> (nom de fichier),
    /// <see cref="IsFrameworkAssembly"/> par défaut. Coûteux : à garder en cache par l'appelant.
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
    /// Référence d'une assembly lue en mémoire, mise en cache par chemin. Un fichier mappé resterait verrouillé
    /// tant que l'application tourne (impossible de supprimer un package du cache).
    /// </summary>
    public static MetadataReference FromFile(string path)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);

        return FileReferences.GetOrAdd(path, p => AssemblyMetadata.CreateFromImage(File.ReadAllBytes(p)).GetReference(filePath: p));
    }
}
