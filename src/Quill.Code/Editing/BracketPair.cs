namespace Quill.Code.Editing;

/// <summary>Matching opening and closing brackets.</summary>
/// <param name="Open">Position of the opening bracket.</param>
/// <param name="Close">Position of the closing bracket.</param>
public readonly record struct BracketPair(int Open, int Close);
