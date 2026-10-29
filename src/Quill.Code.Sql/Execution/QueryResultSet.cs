namespace Quill.Code.Sql.Execution;

/// <summary>Un jeu de resultats (un SELECT) renvoye par l'execution.</summary>
public sealed class QueryResultSet
{
    /// <summary>Position du jeu dans l'execution (base 1).</summary>
    public int Index { get; }

    /// <summary>Colonnes, dans l'ordre du serveur.</summary>
    public IReadOnlyList<QueryResultColumn> Columns { get; }

    /// <summary>Lignes lues, au plus le plafond demande.</summary>
    public IReadOnlyList<QueryResultRow> Rows { get; }

    /// <summary>Vrai si la lecture a ete interrompue au plafond de lignes.</summary>
    public bool IsTruncated { get; }

    private readonly string? _title;

    /// <summary>Titre de l'onglet : celui donne a l'execution (Dump("...")), sinon "Résultats n".</summary>
    public string Title => string.IsNullOrWhiteSpace(_title) ? $"Résultats {Index}" : _title;

    /// <summary>Nombre de lignes lues, pour la barre d'etat.</summary>
    public string Summary => IsTruncated
        ? $"{Rows.Count} premières lignes"
        : $"{Rows.Count} ligne(s)";

    /// <summary>Jeu de resultats ; <paramref name="title"/> remplace "Résultats n".</summary>
    public QueryResultSet(int index, IReadOnlyList<QueryResultColumn> columns, IReadOnlyList<QueryResultRow> rows, bool isTruncated, string? title = null)
    {
        Index = index;
        _title = title;
        Columns = columns;
        Rows = rows;
        IsTruncated = isTruncated;
    }
}
