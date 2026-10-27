namespace Quill.Code.Editing;

/// <summary>Nature d'un caractère du source : les crochets et les paires ne comptent que dans le code.</summary>
public enum SourceRegion : byte
{
    Code = 0,

    LineComment,

    BlockComment,

    /// <summary>Chaîne ou caractère ('...', "...", @"...", """...""").</summary>
    String,
}
