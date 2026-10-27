namespace Quill.Code.Completion;

/// <summary>Proposition de l'autocompletion.</summary>
public sealed record CompletionItem
{
    /// <summary>Texte affiché dans la liste (ex. "Where&lt;&gt;").</summary>
    public required string Label { get; init; }

    /// <summary>Nature du symbole proposé.</summary>
    public required CompletionItemKind Kind { get; init; }

    /// <summary>Texte inséré à la validation ; le libellé à défaut.</summary>
    public string? InsertText { get; init; }

    /// <summary>Texte comparé à la saisie pour filtrer ; le libellé à défaut.</summary>
    public string? FilterText { get; init; }

    /// <summary>Précision affichée à côté du libellé (type d'une colonne, espace de noms…).</summary>
    public string? Detail { get; init; }

    /// <summary>Objet propre au fournisseur (élément Roslyn…), rendu tel quel à la validation.</summary>
    public object? Data { get; init; }

    /// <summary>Texte à insérer : <see cref="InsertText"/>, à défaut <see cref="Label"/>.</summary>
    public string TextToInsert => InsertText ?? Label;

    /// <summary>Texte à filtrer : <see cref="FilterText"/>, à défaut <see cref="Label"/>.</summary>
    public string TextToFilter => FilterText ?? Label;
}
