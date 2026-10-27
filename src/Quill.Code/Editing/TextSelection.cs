namespace Quill.Code.Editing;

/// <summary>Sélection dans un texte : <see cref="Anchor"/> reste fixe, <see cref="Active"/> suit le curseur.</summary>
public readonly record struct TextSelection(int Anchor, int Active)
{
    /// <summary>Curseur sans sélection.</summary>
    public static TextSelection Caret(int position) => new(position, position);

    public int Start => Math.Min(Anchor, Active);

    public int End => Math.Max(Anchor, Active);

    public int Length => End - Start;

    public bool IsEmpty => Anchor == Active;
}
