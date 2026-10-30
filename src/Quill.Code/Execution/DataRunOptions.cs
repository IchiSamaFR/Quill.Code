namespace Quill.Code.Execution;

/// <summary>
/// Réglages d'une exécution qui produit des données (requête SQL, LINQ) : en plus de <see cref="RunOptions.Log"/>,
/// chaque jeu de résultats est remis à l'appelant dès qu'il est lu.
/// </summary>
public record DataRunOptions : RunOptions
{
    /// <summary>
    /// Reçoit chaque jeu de résultats, dans l'ordre de l'exécution, depuis n'importe quel thread.
    /// Null : les données ne sont remises que dans le résultat, s'il y en a un.
    /// </summary>
    public Action<QueryResultSet>? Data { get; init; }
}
