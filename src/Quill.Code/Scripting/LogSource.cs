namespace Quill.Code.Scripting;

/// <summary>Origin of a <see cref="LogEntry"/>, independent of its severity.</summary>
public enum LogSource
{
    /// <summary>Written by the execution host: affected rows, generated SQL, summary, connection error...</summary>
    Host,

    /// <summary>Written by the executed code: a script's <c>Log.*</c>, PRINT and SQL errors of a batch.</summary>
    Code,
}
