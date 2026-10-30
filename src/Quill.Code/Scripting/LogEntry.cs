namespace Quill.Code.Scripting;

/// <summary>
/// Message écrit pendant une exécution, tel que le reçoit <see cref="Execution.RunOptions.Log"/> : à garder ou à
/// afficher par l'application (journal d'un script, onglet Messages d'une requête...).
/// </summary>
/// <param name="Level">Gravité du message.</param>
/// <param name="Text">Texte du message.</param>
/// <param name="Source">Provenance : l'hôte par défaut, <see cref="LogSource.Code"/> pour ce qu'écrit le code exécuté.</param>
public sealed record LogEntry(LogLevel Level, string Text, LogSource Source = LogSource.Host)
{
    /// <summary>Moment de l'écriture ; par défaut, la création du message.</summary>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.Now;

    /// <summary>Vrai pour une erreur (<see cref="LogLevel.Error"/> ou <see cref="LogLevel.Critical"/>).</summary>
    public bool IsError => Level is LogLevel.Error or LogLevel.Critical;
}
