namespace Quill.Code.Sql.SqlServer.Lexing;

/// <summary>Kind of a T-SQL token.</summary>
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
