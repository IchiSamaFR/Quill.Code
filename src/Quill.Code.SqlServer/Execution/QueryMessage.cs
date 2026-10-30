using Quill.Code.Scripting;

namespace Quill.Code.SqlServer.Execution;

/// <summary>Message produit par l'execution : PRINT, Log du script, lignes affectees ou erreur.</summary>
/// <param name="Text">Texte du message.</param>
/// <param name="Kind">Origine du message.</param>
/// <param name="Level">Niveau d'une sortie Log.* du script ; null pour les autres messages.</param>
public sealed record QueryMessage(string Text, QueryMessageKind Kind = QueryMessageKind.Info, LogLevel? Level = null)
{
    /// <summary>Vrai pour un message d'erreur.</summary>
    public bool IsError => Kind == QueryMessageKind.Error;
}
