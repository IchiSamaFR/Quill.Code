using Quill.Code.Scripting;

namespace Quill.Code.Execution;

/// <summary>
/// Settings shared by any code execution (C# script, SQL or LINQ query): output written during
/// execution is handed to the caller as it comes.
/// </summary>
public record RunOptions
{
    /// <summary>
    /// Receives messages written during execution, timestamped and with their origin: a script's <c>Log</c>,
    /// a SQL query's PRINT... Called from any thread. Null: messages are ignored.
    /// </summary>
    public Action<LogEntry>? Log { get; init; }
}
