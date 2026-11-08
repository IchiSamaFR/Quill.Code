namespace Quill.Code.Sql.SqlServer.Completion;

/// <summary>Result of analyzing the text around the caret.</summary>
public sealed class SqlContext
{
    /// <summary>Nothing to suggest at this position.</summary>
    public static SqlContext None { get; } = new() { Kind = SqlContextKind.None };

    /// <summary>Start of statement or undetermined position (language without context analysis).</summary>
    public static SqlContext General { get; } = new() { Kind = SqlContextKind.General };

    /// <summary>Expected kind of input.</summary>
    public required SqlContextKind Kind { get; init; }

    /// <summary>Parts before the dot, in order: ["u"] for u.|, ["db", "dbo"] for db.dbo.|</summary>
    public IReadOnlyList<string> Qualifier { get; init; } = [];

    /// <summary>Context of the qualifier's first word: FROM dbo.| (TableSource) does not expect the same as EXEC dbo.|</summary>
    public SqlContextKind MemberParentKind { get; init; } = SqlContextKind.General;

    /// <summary>Tables of the current batch, the nearest to the caret first.</summary>
    public IReadOnlyList<SqlTableReference> Tables { get; init; } = [];

    /// <summary>Database of the last USE before the caret, otherwise the one given to the analysis; null = connection database.</summary>
    public string? Database { get; init; }

    /// <summary>Table designated by an alias or a simple name (u.| or Users.|).</summary>
    public SqlTableReference? FindTable(string exposedName)
        => Tables.FirstOrDefault(t => string.Equals(t.ExposedName, exposedName, StringComparison.OrdinalIgnoreCase));
}
