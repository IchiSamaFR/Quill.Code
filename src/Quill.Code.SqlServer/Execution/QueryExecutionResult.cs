namespace Quill.Code.SqlServer.Execution;

/// <summary>Bilan complet d'une execution : jeux de resultats, messages et duree.</summary>
public sealed class QueryExecutionResult
{
    /// <summary>Jeux de resultats, dans l'ordre d'execution.</summary>
    public IReadOnlyList<QueryResultSet> ResultSets { get; }

    /// <summary>Messages, dans l'ordre d'emission.</summary>
    public IReadOnlyList<QueryMessage> Messages { get; }

    /// <summary>Duree totale.</summary>
    public TimeSpan Elapsed { get; }

    /// <summary>Vrai si au moins un lot a echoue (les lots suivants sont tout de meme executes).</summary>
    public bool HasErrors { get; }

    /// <summary>Vrai si l'execution a ete annulee.</summary>
    public bool WasCancelled { get; }

    /// <summary>Duree au format SSMS (hh:mm:ss.fff).</summary>
    public string ElapsedLabel => Elapsed.ToString(@"hh\:mm\:ss\.fff", System.Globalization.CultureInfo.InvariantCulture);

    /// <summary>Libelle de fin d'execution pour la barre d'etat.</summary>
    public string StatusLabel => (WasCancelled, HasErrors) switch
    {
        (true, _) => $"Requête annulée ({ElapsedLabel}).",
        (_, true) => $"Requête terminée avec des erreurs ({ElapsedLabel}).",
        _ => $"Requête exécutée avec succès ({ElapsedLabel}).",
    };

    /// <summary>Bilan d'une execution.</summary>
    public QueryExecutionResult(IReadOnlyList<QueryResultSet> resultSets, IReadOnlyList<QueryMessage> messages, TimeSpan elapsed, bool hasErrors, bool wasCancelled)
    {
        ResultSets = resultSets;
        Messages = messages;
        Elapsed = elapsed;
        HasErrors = hasErrors;
        WasCancelled = wasCancelled;
    }
}
