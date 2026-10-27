namespace Quill.Code.Editing;

/// <summary>Crochet ouvrant et crochet fermant associés.</summary>
/// <param name="Open">Position du crochet ouvrant.</param>
/// <param name="Close">Position du crochet fermant.</param>
public readonly record struct BracketPair(int Open, int Close);
