using Quill.Code.Completion;
using System.Text.RegularExpressions;

namespace Quill.Code.SqlServer.Completion;

/// <summary>Mots-clés, types et fonctions T-SQL proposés par l'autocomplétion.</summary>
internal static partial class SqlKeywords
{
    private static readonly string[] Keywords =
    [
        "SELECT", "FROM", "WHERE", "GROUP", "BY", "HAVING", "ORDER", "ASC", "DESC", "TOP", "DISTINCT", "INSERT", "INTO", "VALUES",
        "UPDATE", "SET", "DELETE", "MERGE", "OUTPUT", "TRUNCATE", "CREATE", "ALTER", "DROP", "TABLE", "VIEW", "INDEX", "PROCEDURE",
        "PROC", "FUNCTION", "TRIGGER", "SCHEMA", "DATABASE", "CONSTRAINT", "PRIMARY", "FOREIGN", "KEY", "REFERENCES", "UNIQUE",
        "CHECK", "DEFAULT", "IDENTITY", "CLUSTERED", "NONCLUSTERED", "JOIN", "INNER", "LEFT", "RIGHT", "FULL", "OUTER", "CROSS",
        "APPLY", "ON", "AS", "UNION", "ALL", "EXCEPT", "INTERSECT", "AND", "OR", "NOT", "IN", "EXISTS", "BETWEEN", "LIKE", "IS",
        "NULL", "CASE", "WHEN", "THEN", "ELSE", "END", "IIF", "WITH", "OVER", "PARTITION", "ROWS", "RANGE", "DECLARE", "EXEC",
        "EXECUTE", "RETURN", "BEGIN", "IF", "WHILE", "BREAK", "CONTINUE", "GOTO", "TRY", "CATCH", "THROW", "RAISERROR",
        "TRANSACTION", "TRAN", "COMMIT", "ROLLBACK", "SAVE", "GRANT", "REVOKE", "DENY", "USE", "GO", "OFFSET", "FETCH", "NEXT",
        "ONLY", "FIRST", "NOLOCK", "READUNCOMMITTED",
    ];

    private static readonly string[] DataTypes =
    [
        "INT", "BIGINT", "SMALLINT", "TINYINT", "BIT", "DECIMAL", "NUMERIC", "MONEY", "FLOAT", "REAL", "DATE", "DATETIME",
        "DATETIME2", "DATETIMEOFFSET", "TIME", "CHAR", "VARCHAR", "NCHAR", "NVARCHAR", "TEXT", "NTEXT", "BINARY", "VARBINARY",
        "UNIQUEIDENTIFIER", "XML", "SQL_VARIANT", "MAX",
    ];

    private static readonly string[] Functions =
    [
        "COUNT", "SUM", "AVG", "MIN", "MAX", "CAST", "CONVERT", "TRY_CAST", "TRY_CONVERT", "ISNULL", "COALESCE", "NULLIF", "LEN",
        "SUBSTRING", "REPLACE", "LTRIM", "RTRIM", "TRIM", "UPPER", "LOWER", "CONCAT", "FORMAT", "GETDATE", "SYSDATETIME",
        "DATEADD", "DATEDIFF", "DATEPART", "YEAR", "MONTH", "DAY", "ROW_NUMBER", "RANK", "DENSE_RANK", "NTILE", "LAG", "LEAD",
        "STRING_AGG", "NEWID", "SCOPE_IDENTITY",
    ];

    // Seuls les mots-clés réservés imposent des crochets : une colonne Year ou Date s'écrit telle quelle.
    private static readonly HashSet<string> Reserved = new(Keywords, StringComparer.OrdinalIgnoreCase);

    /// <summary>Mots du langage en propositions (MAX est à la fois type et fonction).</summary>
    public static IReadOnlyList<CompletionItem> Items { get; } =
    [
        .. Keywords.Select(w => new CompletionItem { Label = w, Kind = CompletionItemKind.Keyword }),
        .. DataTypes.Select(w => new CompletionItem { Label = w, Kind = CompletionItemKind.DataType }),
        .. Functions.Select(w => new CompletionItem { Label = w, Kind = CompletionItemKind.Function }),
    ];

    /// <summary>Tous les mots du langage : un mot du document qui en fait partie n'est pas proposé une seconde fois.</summary>
    public static IReadOnlySet<string> Words { get; } = new HashSet<string>([.. Keywords, .. DataTypes, .. Functions], StringComparer.OrdinalIgnoreCase);

    [GeneratedRegex(@"^[\p{L}_][\p{L}\p{Nd}_@#$]*$", RegexOptions.CultureInvariant)]
    private static partial Regex RegularIdentifierRegex();

    /// <summary>Crochets pour un nom irrégulier (espace, tiret…) ou réservé (Order, User).</summary>
    public static string Quote(string name)
        => RegularIdentifierRegex().IsMatch(name) && !Reserved.Contains(name)
            ? name
            : $"[{name.Replace("]", "]]", StringComparison.Ordinal)}]";
}
