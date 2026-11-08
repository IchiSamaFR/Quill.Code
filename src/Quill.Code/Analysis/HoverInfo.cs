namespace Quill.Code.Analysis;

/// <summary>Tooltip for a hovered name.</summary>
/// <param name="Start">Start of the name: the tooltip stays open while the mouse is over it.</param>
/// <param name="Length">Length of the name.</param>
/// <param name="Signature">Main line (e.g. "(property) string Student.Name").</param>
/// <param name="Description">Optional detail (documentation, source table…).</param>
public sealed record HoverInfo(int Start, int Length, string Signature, string? Description = null)
{
    /// <summary>End of the name.</summary>
    public int End => Start + Length;
}
