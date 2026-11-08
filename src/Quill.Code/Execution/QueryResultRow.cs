namespace Quill.Code.Execution;

/// <summary>
/// Row of a result set, already converted to displayable text.
/// A null cell represents a SQL NULL.
/// </summary>
/// <param name="Number">Row number (1-based).</param>
/// <param name="Cells">Values, in column order.</param>
public sealed record QueryResultRow(int Number, string?[] Cells);
