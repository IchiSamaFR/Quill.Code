namespace Quill.Code.Highlighting;

/// <summary>Mot d'une liste <c>Keywords</c> du .xshd, réutilisable par l'autocomplétion.</summary>
/// <param name="Word">Mot tel qu'écrit dans le fichier.</param>
/// <param name="Category">Nom de la couleur de la liste (Keyword, DataType, Function…).</param>
public sealed record SyntaxKeyword(string Word, string Category);
