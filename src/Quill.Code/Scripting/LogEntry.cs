namespace Quill.Code.Scripting;

/// <summary>
/// Message written during an execution, as received by <see cref="Execution.RunOptions.Log"/>: to be kept or
/// displayed by the application (script log, Messages tab of a query...).
/// </summary>
/// <param name="Level">Severity of the message.</param>
/// <param name="Text">Text of the message.</param>
/// <param name="Source">Origin: the host by default, <see cref="LogSource.Code"/> for what the executed code writes.</param>
public sealed record LogEntry(LogLevel Level, string Text, LogSource Source = LogSource.Host)
{
    /// <summary>Time of writing; by default, the creation of the message.</summary>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.Now;

    /// <summary>True for an error (<see cref="LogLevel.Error"/> or <see cref="LogLevel.Critical"/>).</summary>
    public bool IsError => Level is LogLevel.Error or LogLevel.Critical;
}
