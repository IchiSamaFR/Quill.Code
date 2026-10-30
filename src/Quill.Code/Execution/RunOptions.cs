using Quill.Code.Scripting;

namespace Quill.Code.Execution;

/// <summary>
/// Réglages communs à toute exécution de code (script C#, requête SQL ou LINQ) : la sortie écrite pendant
/// l'exécution est remise à l'appelant au fil de l'eau.
/// </summary>
public record RunOptions
{
    /// <summary>
    /// Reçoit les messages (niveau, texte) écrits pendant l'exécution : <c>Log</c> d'un script, PRINT d'une
    /// requête SQL... Appelé depuis n'importe quel thread. Null : les messages sont ignorés.
    /// </summary>
    public Action<LogLevel, string>? Log { get; init; }
}
