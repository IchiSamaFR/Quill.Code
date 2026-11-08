namespace Quill.Code.Sql.SqlServer.Constants;

/// <summary>T-SQL analysis constants (diagnostics, hover).</summary>
internal static class SqlAnalysisConstants
{
    /// <summary>After a first error, the parser resynchronizes poorly: the following ones are often echoes.</summary>
    internal const int MaxSyntaxDiagnostics = 20;

    /// <summary>Columns listed in a table's tooltip: beyond that, it would overflow the editor.</summary>
    internal const int HoverMaxListedColumns = 12;
}
