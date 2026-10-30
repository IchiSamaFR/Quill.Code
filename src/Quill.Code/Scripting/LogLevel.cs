namespace Quill.Code.Scripting;

/// <summary>
/// Niveau d'un message écrit pendant une exécution (<c>Log</c> d'un script, PRINT et erreurs SQL...), calqué sur
/// <c>Microsoft.Extensions.Logging.LogLevel</c> avec en plus <see cref="Information"/> (les valeurs numériques diffèrent donc
/// à partir de <see cref="Warning"/>). Ses noms et son ordre forment aussi l'enum <c>LogLevel</c> généré dans les scripts.
/// </summary>
public enum LogLevel
{
    /// <summary>Messages les plus détaillés.</summary>
    Trace,

    /// <summary>Investigation pendant la mise au point.</summary>
    Debug,

    /// <summary>Déroulement général du script.</summary>
    Information,

    /// <summary>Événement anormal ou inattendu qui n'arrête pas le script.</summary>
    Warning,

    /// <summary>Échec de l'opération en cours.</summary>
    Error,

    /// <summary>Échec grave qui demande une attention immédiate.</summary>
    Critical,

    /// <summary>N'écrit rien.</summary>
    None,
}
