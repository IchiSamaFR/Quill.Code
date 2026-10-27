using System.Text;

namespace Quill.Code.Editing;

/// <summary>
/// Résultat d'une commande d'édition : remplacements exprimés dans le texte d'origine (ordonnés, sans chevauchement),
/// à appliquer en une seule étape d'annulation, et sélection dans le texte modifié.
/// </summary>
public sealed record EditResult(IReadOnlyList<TextEdit> Edits, TextSelection Selection)
{
    /// <summary>Applique les remplacements à <paramref name="text"/>.</summary>
    public string Apply(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        StringBuilder builder = new(text.Length);
        var position = 0;
        foreach (var edit in Edits)
        {
            builder.Append(text, position, edit.Start - position).Append(edit.NewText);
            position = edit.End;
        }

        return builder.Append(text, position, text.Length - position).ToString();
    }

    /// <summary>
    /// Remplacement unique couvrant tous ceux du résultat (pour un contrôle qui n'applique qu'une portion à la fois),
    /// null s'il n'y a rien à remplacer (simple déplacement du curseur).
    /// </summary>
    public TextEdit? ToSingleEdit(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        switch (Edits.Count)
        {
            case 0:
                return null;
            case 1:
                return Edits[0];
        }

        var start = Edits[0].Start;
        var end = Edits[^1].End;
        var applied = Apply(text);
        var length = applied.Length - (text.Length - (end - start));
        return new TextEdit(start, end - start, applied.Substring(start, length));
    }

    internal static EditResult Replace(int start, int length, string newText, TextSelection selection)
        => new([new TextEdit(start, length, newText)], selection);

    internal static EditResult Move(TextSelection selection) => new([], selection);
}
