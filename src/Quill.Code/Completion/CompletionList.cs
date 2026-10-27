namespace Quill.Code.Completion;

/// <summary>Résultat d'une demande d'autocompletion.</summary>
/// <param name="Items">Propositions, dans l'ordre de tri du fournisseur.</param>
/// <param name="SpanStart">Début de la portion que la proposition validée remplacera (mot en cours de frappe).</param>
/// <param name="SpanLength">Longueur de cette portion au moment de la demande.</param>
/// <param name="IsSuggestionMode">
/// Vrai quand l'utilisateur nomme quelque chose (déclaration) : aucune proposition ne doit être validée automatiquement.
/// </param>
public sealed record CompletionList(IReadOnlyList<CompletionItem> Items, int SpanStart, int SpanLength, bool IsSuggestionMode = false)
{
    /// <summary>Liste vide.</summary>
    public static CompletionList Empty { get; } = new([], 0, 0);

    /// <summary>Fin de la portion remplacée.</summary>
    public int SpanEnd => SpanStart + SpanLength;
}
