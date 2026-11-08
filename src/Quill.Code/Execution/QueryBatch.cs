namespace Quill.Code.Execution;

/// <summary>Batch of a script, executed as one command.</summary>
/// <param name="Text">Text sent to the server.</param>
/// <param name="StartLine">Script line (1-based) where the batch starts, to locate its errors.</param>
public sealed record QueryBatch(string Text, int StartLine);
