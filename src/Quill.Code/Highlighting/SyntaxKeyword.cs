namespace Quill.Code.Highlighting;

/// <summary>Word from a <c>Keywords</c> list of the .xshd, reusable by completion.</summary>
/// <param name="Word">Word as written in the file.</param>
/// <param name="Category">Color name of the list (Keyword, DataType, Function…).</param>
public sealed record SyntaxKeyword(string Word, string Category);
