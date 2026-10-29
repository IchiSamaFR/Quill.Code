namespace Quill.Code.Sql.Execution;

/// <summary>Origine d'un message d'execution, pour le distinguer dans l'onglet Messages.</summary>
public enum QueryMessageKind
{
    /// <summary>Message du moteur ou de l'application : duree, lignes affectees, SQL genere...</summary>
    Info,

    /// <summary>Erreur du moteur ou de l'execution.</summary>
    Error,

    /// <summary>Sortie ecrite par le script lui-meme (Log.*, PRINT).</summary>
    Output,
}
