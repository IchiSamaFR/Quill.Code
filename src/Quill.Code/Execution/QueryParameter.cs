namespace Quill.Code.Execution;

/// <summary>Parameter sent with a query (null = SQL NULL), never concatenated into the text.</summary>
public sealed record QueryParameter(string Name, QueryParameterType Type, object? Value);
