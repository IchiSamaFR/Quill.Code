namespace Quill.Code.Sql.SqlServer.Execution;

/// <summary>T-SQL batch delimited by GO.</summary>
/// <param name="Text">Text of the batch, without the GO line.</param>
/// <param name="StartLine">Script line where the batch starts (1-based), to realign errors.</param>
/// <param name="RepeatCount">Number of executions requested by "GO n".</param>
internal sealed record SqlBatch(string Text, int StartLine, int RepeatCount);
