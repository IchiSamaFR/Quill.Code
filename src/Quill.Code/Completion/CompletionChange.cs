namespace Quill.Code.Completion;

/// <summary>Modification à appliquer au texte quand une proposition est validée.</summary>
/// <param name="SpanStart">Début de la portion remplacée.</param>
/// <param name="SpanLength">Longueur de la portion remplacée.</param>
/// <param name="NewText">Texte inséré à la place.</param>
/// <param name="NewCaretPosition">Position du curseur dans le nouveau texte ; fin de l'insertion si nulle.</param>
public sealed record CompletionChange(int SpanStart, int SpanLength, string NewText, int? NewCaretPosition = null)
{
    /// <summary>Position du curseur après application de la modification.</summary>
    public int CaretPosition => NewCaretPosition ?? SpanStart + NewText.Length;
}
