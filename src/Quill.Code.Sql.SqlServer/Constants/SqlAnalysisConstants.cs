namespace Quill.Code.Sql.SqlServer.Constants;

/// <summary>Constantes de l'analyse T-SQL (diagnostics, survol).</summary>
internal static class SqlAnalysisConstants
{
    /// <summary>Apres une premiere faute, le parseur se resynchronise mal : les suivantes sont souvent des echos.</summary>
    internal const int MaxSyntaxDiagnostics = 20;

    /// <summary>Colonnes listées dans l'infobulle d'une table : au-delà, elle déborderait de l'éditeur.</summary>
    internal const int HoverMaxListedColumns = 12;
}
