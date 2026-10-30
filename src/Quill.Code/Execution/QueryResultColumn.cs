namespace Quill.Code.Execution;

/// <summary>Colonne d'un jeu de resultats.</summary>
/// <param name="Name">Nom renvoye par le serveur, ou un libelle de substitution si la colonne n'en a pas.</param>
/// <param name="TypeName">Type SQL de la colonne (ex. "nvarchar").</param>
public sealed record QueryResultColumn(string Name, string TypeName);
