using Quill.Code.Execution;
using Quill.Data.Sql;

namespace Quill.Code.CSharp.Linq;

/// <summary>Contexte d'un <see cref="LinqLanguageService"/> : ce qui donne un sens aux noms de la requete.</summary>
public sealed record LinqLanguageOptions
{
    /// <summary>Moteur de la base (ex. <c>SqlServerLinqProvider</c>) ; null : <see cref="Schema"/> est ignoree, C# seul.</summary>
    public ILinqDatabaseProvider? Provider { get; init; }

    /// <summary>
    /// Structure de la base interrogee, relue a chaque demande (une connexion qui s'ouvre enrichit les reponses sans
    /// recreer le service) ; null, une structure nulle ou une exception : C# seul, <c>Db</c> etant un DbContext vide.
    /// Une meme instance donne le meme DbContext compile : la lire depuis un cache.
    /// </summary>
    public Func<CancellationToken, Task<SqlDatabaseSchema?>>? Schema { get; init; }

    /// <summary>Parametres fournis a l'execution, utilisables par leur nom (sans @) ; seuls les noms et types comptent ici.</summary>
    public IReadOnlyList<QueryParameter> Parameters { get; init; } = [];
}
