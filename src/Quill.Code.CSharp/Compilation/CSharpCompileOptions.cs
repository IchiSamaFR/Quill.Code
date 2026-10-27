using System.Reflection;

namespace Quill.Code.CSharp.Compilation;

/// <summary>Réglages de <see cref="ICSharpLanguageService.CompileAsync(string, CSharpCompileOptions?, CancellationToken)"/>.</summary>
public sealed record CSharpCompileOptions
{
    /// <summary>Nom de l'assembly émise ; par défaut le nom du projet suivi d'un identifiant unique.</summary>
    public string? AssemblyName { get; init; }

    /// <summary>Émet aussi un PDB portable : numéros de ligne dans les piles d'appels des exceptions.</summary>
    public bool EmitDebugInformation { get; init; }

    /// <summary>
    /// Assemblies (fichiers) que le code peut charger, résolues par nom et lues en mémoire pour ne pas verrouiller
    /// les fichiers (ex. packages d'un script).
    /// </summary>
    public IEnumerable<string> AssemblyFiles { get; init; } = [];

    /// <summary>
    /// Résolution des autres assemblies demandées par le code, avant <see cref="AssemblyFiles"/> (ex. modèle déjà
    /// chargé dans un autre contexte) ; null : le runtime les fournit.
    /// </summary>
    public Func<AssemblyName, Assembly?>? ResolveAssembly { get; init; }
}
