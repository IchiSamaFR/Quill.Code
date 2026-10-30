namespace Quill.Code.Scripting;

/// <summary>
/// Message écrit pendant une exécution, tel que le reçoit <see cref="Execution.RunOptions.Log"/> : à garder ou à
/// afficher par l'application (journal d'un script, onglet Messages d'une requête...).
/// </summary>
/// <param name="Level">Niveau du message ; <see cref="LogLevel.Information"/> pour la sortie du code.</param>
/// <param name="Text">Texte du message.</param>
public sealed record LogEntry(LogLevel Level, string Text)
{
    /// <summary>Vrai pour une erreur (<see cref="LogLevel.Error"/> ou <see cref="LogLevel.Critical"/>).</summary>
    public bool IsError => Level is LogLevel.Error or LogLevel.Critical;
}
