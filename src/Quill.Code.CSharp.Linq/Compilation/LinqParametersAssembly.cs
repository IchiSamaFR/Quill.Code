using Microsoft.CodeAnalysis;
using System.Reflection;

namespace Quill.Code.CSharp.Linq.Compilation;

/// <summary>
/// QueryGlobals compile et charge : le contexte (Db) et un membre par parametre de la requete, types mais sans valeur.
/// Reutilisable par toutes les requetes d'un meme modele et d'un meme jeu de parametres (noms et types) ; les valeurs
/// sont posees a chaque execution (proprietes en AsyncLocal, propres a chaque flux).
/// </summary>
internal sealed class LinqParametersAssembly
{
    /// <summary>Modele contre lequel QueryGlobals est compile ; null : C# seul (Db est un DbContext).</summary>
    public LinqGeneratedModelAssembly? Model { get; init; }

    public required Assembly Assembly { get; init; }

    /// <summary>Propriete Db de QueryGlobals, posee avant chaque execution.</summary>
    public required PropertyInfo ContextProperty { get; init; }

    /// <summary>Proprietes des parametres, par nom expose (sans @), posees avant chaque execution.</summary>
    public required IReadOnlyDictionary<string, PropertyInfo> ParameterProperties { get; init; }

    /// <summary>Reference Roslyn vers l'image compilee, pour compiler les requetes contre elle.</summary>
    public required MetadataReference Reference { get; init; }
}
