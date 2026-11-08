namespace Quill.Code.Editing;

/// <summary>Selection in a text: <see cref="Anchor"/> stays fixed, <see cref="Active"/> follows the caret.</summary>
public readonly record struct TextSelection(int Anchor, int Active)
{
    /// <summary>Caret without selection.</summary>
    public static TextSelection Caret(int position) => new(position, position);

    public int Start => Math.Min(Anchor, Active);

    public int End => Math.Max(Anchor, Active);

    public int Length => End - Start;

    public bool IsEmpty => Anchor == Active;
}
