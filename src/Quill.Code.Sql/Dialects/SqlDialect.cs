using System.Globalization;
using Quill.Code.Sql.Constants;

namespace Quill.Code.Sql.Dialects;

/// <summary>
/// What sets an engine's SQL apart for the editor: identifier delimiters, parameter prefix, reading the first rows,
/// language words. Each <c>Quill.Code.Sql.[Engine]</c> exposes its own (<c>OracleDialect.Instance</c>…).
/// </summary>
public sealed class SqlDialect
{
    private HashSet<string>? _reserved;
    private HashSet<string>? _words;

    /// <summary>Displayed name (e.g. "Oracle").</summary>
    public required string Name { get; init; }

    /// <summary>Recognized identifier delimiters; the first one is used to write a name.</summary>
    public IReadOnlyList<SqlIdentifierQuote> IdentifierQuotes { get; init; } = [new SqlIdentifierQuote('"', '"')];

    /// <summary>Casing of an identifier written without delimiters.</summary>
    public SqlIdentifierCasing IdentifierCasing { get; init; } = SqlIdentifierCasing.Insensitive;

    /// <summary>Characters allowed in an undelimited name, besides letters, digits and <c>_</c>.</summary>
    public string ExtraIdentifierChars { get; init; } = "$";

    /// <summary>True if <c>"text"</c> is a string rather than an identifier (MySQL).</summary>
    public bool DoubleQuotedStrings { get; init; }

    /// <summary>Prefix of a named parameter: <c>@name</c> or <c>:name</c>.</summary>
    public char ParameterPrefix { get; init; } = '@';

    /// <summary>Reading the first rows.</summary>
    public SqlRowLimitSyntax RowLimit { get; init; } = SqlRowLimitSyntax.Limit;

    /// <summary>True if an object is named database.schema.object (SQL Server).</summary>
    public bool QualifiesWithDatabase { get; init; }

    /// <summary>True if an object's schema is its database (MySQL, SQLite): database.object.</summary>
    public bool SchemaIsDatabase { get; init; }

    /// <summary>True if a query can read another database of the same connection.</summary>
    public bool QueriesOtherDatabases { get; init; }

    /// <summary>Schema of unqualified names (<c>dbo</c>, <c>public</c>); null if it depends on the connection.</summary>
    public string? DefaultSchema { get; init; }

    /// <summary>Word that, alone on its line, separates two batches (<c>GO</c>); null if not applicable.</summary>
    public string? BatchSeparator { get; init; }

    /// <summary>True if <c>USE database</c> changes the current database.</summary>
    public bool HasUseStatement { get; init; }

    /// <summary>Words followed by a procedure name (<c>EXEC</c>, <c>CALL</c>).</summary>
    public IReadOnlyList<string> ProcedureKeywords { get; init; } = ["CALL"];

    /// <summary>True if <c>#</c> starts a line comment (MySQL).</summary>
    public bool HashLineComments { get; init; }

    /// <summary>True if <c>/* */</c> comments nest.</summary>
    public bool NestedBlockComments { get; init; }

    /// <summary>True if <c>$$text$$</c> and <c>$tag$text$tag$</c> are strings (PostgreSQL).</summary>
    public bool DollarQuotedStrings { get; init; }

    /// <summary>Suggested keywords; an identifier that is one of them gets delimited.</summary>
    public IReadOnlyList<string> Keywords { get; init; } = SqlStandardKeywords.Keywords;

    /// <summary>Suggested data types.</summary>
    public IReadOnlyList<string> DataTypes { get; init; } = SqlStandardKeywords.DataTypes;

    /// <summary>Suggested functions.</summary>
    public IReadOnlyList<string> Functions { get; init; } = SqlStandardKeywords.Functions;

    /// <summary>Dialect-specific words that may follow a table name (<c>LIMIT</c>, <c>FETCH</c>): never an alias.</summary>
    public IReadOnlyList<string> ClauseKeywords { get; init; } = [];

    /// <summary>Multi-word phrases suggested as is (<c>FETCH FIRST 10 ROWS ONLY</c>).</summary>
    public IReadOnlyList<string> Phrases { get; init; } = [];

    /// <summary>True for a keyword of the dialect.</summary>
    public bool IsReserved(string word)
    {
        ArgumentNullException.ThrowIfNull(word);
        return (_reserved ??= new HashSet<string>(Keywords, StringComparer.OrdinalIgnoreCase)).Contains(word);
    }

    /// <summary>True for a language word: keyword, type or function.</summary>
    public bool IsLanguageWord(string word)
    {
        ArgumentNullException.ThrowIfNull(word);
        return (_words ??= new HashSet<string>([.. Keywords, .. DataTypes, .. Functions], StringComparer.OrdinalIgnoreCase)).Contains(word);
    }

    /// <summary>
    /// Name as written in a query: delimited if it is irregular (space, dash…), reserved, or in a casing the engine would not
    /// keep; always delimited if <paramref name="always"/>.
    /// </summary>
    public string QuoteIdentifier(string name, bool always = false)
    {
        ArgumentNullException.ThrowIfNull(name);

        if (!always && IsRegularIdentifier(name) && !IsReserved(name))
            return name;

        var quote = IdentifierQuotes[0];
        var close = quote.Close.ToString();
        return $"{quote.Open}{name.Replace(close, close + close, StringComparison.Ordinal)}{quote.Close}";
    }

    /// <summary>Qualified name of an object: database.schema.object, database.object or schema.object depending on the engine.</summary>
    public string FormatObjectName(string? database, string? schema, string name, bool alwaysQuote = false)
    {
        ArgumentNullException.ThrowIfNull(name);

        var result = QuoteIdentifier(name, alwaysQuote);
        if (string.IsNullOrWhiteSpace(schema))
            return result;

        result = $"{QuoteIdentifier(schema, alwaysQuote)}.{result}";
        return QualifiesWithDatabase && !string.IsNullOrWhiteSpace(database)
            ? $"{QuoteIdentifier(database, alwaysQuote)}.{result}"
            : result;
    }

    /// <summary>Parameter as written in a query, from its name with or without a prefix.</summary>
    public string FormatParameter(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        return $"{ParameterPrefix}{name.Trim().TrimStart('@', ':')}";
    }

    /// <summary>Query reading the first <paramref name="count"/> rows of <paramref name="source"/> (name already formatted).</summary>
    public string SelectFirstRows(string source, int count)
    {
        ArgumentNullException.ThrowIfNull(source);

        var rows = count.ToString(CultureInfo.InvariantCulture);
        return RowLimit switch
        {
            SqlRowLimitSyntax.Top => $"SELECT TOP ({rows}) *\nFROM {source};",
            SqlRowLimitSyntax.FetchFirst => $"SELECT *\nFROM {source}\nFETCH FIRST {rows} ROWS ONLY;",
            _ => $"SELECT *\nFROM {source}\nLIMIT {rows};",
        };
    }

    private bool IsRegularIdentifier(string name)
    {
        if (name.Length == 0 || !(char.IsLetter(name[0]) || name[0] == '_'))
            return false;

        foreach (var c in name)
        {
            if (!char.IsLetterOrDigit(c) && c != '_' && !ExtraIdentifierChars.Contains(c, StringComparison.Ordinal))
                return false;
        }

        return IdentifierCasing switch
        {
            SqlIdentifierCasing.Upper => !name.Any(char.IsLower),
            SqlIdentifierCasing.Lower => !name.Any(char.IsUpper),
            _ => true,
        };
    }
}
