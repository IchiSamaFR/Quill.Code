namespace Quill.Code.Sql.Lexing;

/// <summary>Nature d'un jeton T-SQL.</summary>
public enum SqlTokenKind
{
    Word,
    QuotedIdentifier,
    String,
    LineComment,
    BlockComment,
    Number,
    Variable,
    Dot,
    Comma,
    OpenParen,
    CloseParen,
    Semicolon,
    Operator
}
