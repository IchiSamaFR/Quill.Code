namespace Quill.Code.Sql.SqlServer.Lexing;

/// <summary>Jeton T-SQL : nature et position dans le texte.</summary>
/// <param name="Kind">Nature du jeton.</param>
/// <param name="Start">Début dans le texte.</param>
/// <param name="Length">Longueur.</param>
/// <param name="IsTerminated">Faux pour une chaine, un commentaire ou un [identifiant] non referme.</param>
public readonly record struct SqlToken(SqlTokenKind Kind, int Start, int Length, bool IsTerminated = true)
{
    public int End => Start + Length;

    public bool IsComment => Kind is SqlTokenKind.LineComment or SqlTokenKind.BlockComment;

    public string GetText(string text) => text.Substring(Start, Length);

    public bool IsWord(string text, string word)
        => Kind == SqlTokenKind.Word && Length == word.Length && string.Compare(text, Start, word, 0, Length, StringComparison.OrdinalIgnoreCase) == 0;
}
