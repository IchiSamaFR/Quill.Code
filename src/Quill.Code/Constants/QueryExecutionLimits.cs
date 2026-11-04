namespace Quill.Code.Constants;

/// <summary>Plafonds communs aux executeurs de requetes.</summary>
public static class QueryExecutionLimits
{
    /// <summary>Plafond par jeu de resultats, pour ne pas saturer la memoire ni la grille.</summary>
    public const int MaxRowsPerResultSet = 10_000;

    /// <summary>Delai d'ouverture d'une connexion pour une requete parametree (cartes, graphiques) : un serveur injoignable ne doit pas figer l'affichage.</summary>
    public const int ReadConnectTimeoutSeconds = 8;

    /// <summary>Delai d'execution d'une requete parametree.</summary>
    public const int ReadCommandTimeoutSeconds = 120;
}
