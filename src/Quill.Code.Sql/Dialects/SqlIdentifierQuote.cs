namespace Quill.Code.Sql.Dialects;

/// <summary>Identifier delimiters: <c>[name]</c>, <c>"name"</c>, <c>`name`</c>.</summary>
/// <param name="Open">Opening character.</param>
/// <param name="Close">Closing character, doubled to write it inside the name.</param>
public readonly record struct SqlIdentifierQuote(char Open, char Close);
