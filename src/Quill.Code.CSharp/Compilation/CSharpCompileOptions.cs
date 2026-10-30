using Quill.Code.Execution;
using System.Reflection;

namespace Quill.Code.CSharp.Compilation;

/// <summary>
/// Réglages de <see cref="CSharpLanguageService.CompileAsync(string, CSharpCompileOptions?, CancellationToken)"/> et de
/// <see cref="CSharpLanguageService.RunAsync"/> ; <see cref="RunOptions.Log"/> reçoit les messages <c>Log</c> du code
/// compilé, propres à cette compilation, et demande <see cref="CSharpLanguageOptions.IncludeScriptRuntime"/>.
/// </summary>
public sealed record CSharpCompileOptions : RunOptions
{
    /// <summary>Nom de l'assembly émise ; par défaut le nom du projet suivi d'un identifiant unique.</summary>
    public string? AssemblyName { get; init; }

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

    /// <summary>
    /// Faux pour charger l'assembly pour toute la vie de l'application (ex. modèle dont une bibliothèque garde les
    /// types en cache) : <see cref="CSharpCompilationResult.Dispose"/> ne la décharge alors pas.
    /// </summary>
    public bool Collectible { get; init; } = true;
}
