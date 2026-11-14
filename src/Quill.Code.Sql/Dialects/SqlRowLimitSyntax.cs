namespace Quill.Code.Sql.Dialects;

/// <summary>How to read only the first rows of a query.</summary>
public enum SqlRowLimitSyntax
{
    /// <summary><c>SELECT TOP (n) …</c> (SQL Server).</summary>
    Top,

    /// <summary><c>… LIMIT n</c> (MySQL, PostgreSQL, SQLite).</summary>
    Limit,

    /// <summary><c>… FETCH FIRST n ROWS ONLY</c> (Oracle 12c and later).</summary>
    FetchFirst,
}
