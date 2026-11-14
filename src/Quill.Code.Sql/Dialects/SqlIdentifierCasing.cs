namespace Quill.Code.Sql.Dialects;

/// <summary>Casing the engine gives to an identifier written without delimiters.</summary>
public enum SqlIdentifierCasing
{
    /// <summary>Case-insensitive: a regular name never needs delimiters (SQL Server, MySQL, SQLite).</summary>
    Insensitive,

    /// <summary>Folded to upper case (Oracle): a name containing a lower-case letter must be delimited.</summary>
    Upper,

    /// <summary>Folded to lower case (PostgreSQL): a name containing an upper-case letter must be delimited.</summary>
    Lower,
}
