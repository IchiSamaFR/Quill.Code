using Microsoft.CodeAnalysis;
using Quill.Code.CSharp.Linq.Modeling;
using System.Reflection;

namespace Quill.Code.CSharp.Linq.Compilation;

/// <summary>Modele compile et charge, reutilisable par toutes les requetes sur ce schema.</summary>
internal sealed class LinqGeneratedModelAssembly
{
    public required EfGeneratedModel Model { get; init; }

    /// <summary>Moteur du modele : connexion du contexte EF.</summary>
    public required ILinqDatabaseProvider Provider { get; init; }

    /// <summary>References de la plateforme, d'EF et du fournisseur, communes au modele et a ses requetes.</summary>
    public required IReadOnlyList<MetadataReference> PlatformReferences { get; init; }

    public required Assembly Assembly { get; init; }

    public required Type ContextType { get; init; }

    /// <summary>Destination des Dump() de la requete en cours (propriete statique generee, propre a chaque execution).</summary>
    public required PropertyInfo DumpSinkProperty { get; init; }

    /// <summary>Reference Roslyn vers l'image compilee, pour compiler les requetes contre elle.</summary>
    public required MetadataReference Reference { get; init; }
}
