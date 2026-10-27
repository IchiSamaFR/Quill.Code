namespace Quill.Code.Highlighting;

/// <summary>
/// Mise en forme d'une catégorie de texte (élément <c>Color</c> d'un .xshd), telle qu'écrite dans le fichier :
/// chaque application la convertit pour son framework (Brush WPF, Color MAUI…).
/// </summary>
/// <param name="Name">Nom de la couleur (« Comment », « Keyword »…) ; null pour une mise en forme déclarée sur la règle.</param>
/// <param name="Foreground">Couleur du texte (« #569CD6 », « Green »…) ; héritée de l'éditeur si nulle.</param>
/// <param name="FontWeight">Graisse (« Bold »…) ; héritée si nulle.</param>
/// <param name="FontStyle">Style (« Italic »…) ; hérité si nul.</param>
public sealed record SyntaxColor(string? Name, string? Foreground, string? FontWeight = null, string? FontStyle = null);
