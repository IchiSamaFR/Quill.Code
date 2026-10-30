namespace Quill.Code.Scripting;

/// <summary>Provenance d'un <see cref="LogEntry"/>, indépendante de sa gravité.</summary>
public enum LogSource
{
    /// <summary>Écrit par l'hôte de l'exécution : lignes affectées, SQL généré, bilan, erreur de connexion...</summary>
    Host,

    /// <summary>Écrit par le code exécuté : <c>Log.*</c> d'un script, PRINT et erreurs SQL d'un lot.</summary>
    Code,
}
