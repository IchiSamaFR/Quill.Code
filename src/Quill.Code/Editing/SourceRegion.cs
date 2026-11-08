namespace Quill.Code.Editing;

/// <summary>Kind of a source character: brackets and pairs only count in code.</summary>
public enum SourceRegion : byte
{
    Code = 0,

    LineComment,

    BlockComment,

    /// <summary>String or character ('...', "...", @"...", """...""").</summary>
    String,
}
