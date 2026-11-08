namespace Quill.Code.Execution;

/// <summary>
/// Settings for an execution that produces data (SQL query, LINQ): in addition to <see cref="RunOptions.Log"/>,
/// each result set is handed to the caller as soon as it is read.
/// </summary>
public record DataRunOptions : RunOptions
{
    /// <summary>
    /// Receives each result set, in execution order, from any thread.
    /// Null: data is only returned in the result, if there is one.
    /// </summary>
    public Action<QueryResultSet>? Data { get; init; }
}
