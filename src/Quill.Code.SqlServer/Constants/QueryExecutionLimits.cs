namespace Quill.Code.SqlServer.Constants;

/// <summary>Plafonds communs aux executeurs de requetes.</summary>
public static class QueryExecutionLimits
{
    /// <summary>Plafond par jeu de resultats, pour ne pas saturer la memoire ni la grille.</summary>
    public const int MaxRowsPerResultSet = 10_000;
}
