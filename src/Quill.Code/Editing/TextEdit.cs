namespace Quill.Code.Editing;

/// <summary>Remplacement d'une portion de texte.</summary>
/// <param name="Start">Début de la portion remplacée.</param>
/// <param name="Length">Longueur de la portion remplacée (0 pour une insertion).</param>
/// <param name="NewText">Texte inséré à la place (vide pour une suppression).</param>
public sealed record TextEdit(int Start, int Length, string NewText)
{
    /// <summary>Fin de la portion remplacée.</summary>
    public int End => Start + Length;

    /// <summary>
    /// Plus petit remplacement qui transforme <paramref name="oldText"/> en <paramref name="newText"/>
    /// (préfixe et suffixe communs conservés), null si les textes sont égaux.
    /// </summary>
    public static TextEdit? Between(string oldText, string newText)
    {
        ArgumentNullException.ThrowIfNull(oldText);
        ArgumentNullException.ThrowIfNull(newText);

        var prefix = oldText.AsSpan().CommonPrefixLength(newText);
        if (prefix == oldText.Length && prefix == newText.Length)
            return null;

        var maxSuffix = Math.Min(oldText.Length, newText.Length) - prefix;
        var suffix = 0;
        while (suffix < maxSuffix && oldText[oldText.Length - 1 - suffix] == newText[newText.Length - 1 - suffix])
            suffix++;

        return new TextEdit(prefix, oldText.Length - prefix - suffix, newText.Substring(prefix, newText.Length - prefix - suffix));
    }
}
