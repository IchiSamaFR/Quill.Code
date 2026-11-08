namespace Quill.Code.Sql.SqlServer.Lexing;

/// <summary>T-SQL token: kind and position in the text.</summary>
/// <param name="Kind">Kind of the token.</param>
/// <param name="Start">Start in the text.</param>
/// <param name="Length">Length.</param>
/// <param name="IsTerminated">False for an unterminated string, comment or [identifier].</param>
public readonly record struct SqlToken(SqlTokenKind Kind, int Start, int Length, bool IsTerminated = true)
{
    public int End => Start + Length;

    public bool IsComment => Kind is SqlTokenKind.LineComment or SqlTokenKind.BlockComment;

    public string GetText(string text) => text.Substring(Start, Length);

    public bool IsWord(string text, string word)
        => Kind == SqlTokenKind.Word && Length == word.Length && string.Compare(text, Start, word, 0, Length, StringComparison.OrdinalIgnoreCase) == 0;
}
