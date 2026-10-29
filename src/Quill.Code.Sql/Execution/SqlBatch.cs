namespace Quill.Code.Sql.Execution;

/// <summary>Lot T-SQL delimite par GO.</summary>
/// <param name="Text">Texte du lot, sans la ligne GO.</param>
/// <param name="StartLine">Ligne du script ou commence le lot (base 1), pour recaler les erreurs.</param>
/// <param name="RepeatCount">Nombre d'executions demande par "GO n".</param>
public sealed record SqlBatch(string Text, int StartLine, int RepeatCount);
