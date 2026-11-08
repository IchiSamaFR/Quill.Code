namespace Quill.Code.Editing;

/// <summary>Replacement of a span of text.</summary>
/// <param name="Start">Start of the replaced span.</param>
/// <param name="Length">Length of the replaced span (0 for an insertion).</param>
/// <param name="NewText">Text inserted in its place (empty for a deletion).</param>
public sealed record TextEdit(int Start, int Length, string NewText)
{
    /// <summary>End of the replaced span.</summary>
    public int End => Start + Length;

    /// <summary>
    /// Smallest replacement turning <paramref name="oldText"/> into <paramref name="newText"/>
    /// (common prefix and suffix kept), null if the texts are equal.
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
