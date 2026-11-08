namespace Quill.Code.Analysis;

/// <summary>Overloads of the method whose argument list contains the caret.</summary>
/// <param name="Signatures">Available overloads.</param>
/// <param name="ActiveSignature">Overload chosen by the compiler, otherwise the first one accepting enough arguments.</param>
/// <param name="ActiveParameter">Index of the argument under the caret (number of commas before it).</param>
/// <param name="ArgumentListStart">Position of the opening parenthesis: identifies the call from one keystroke to the next.</param>
public sealed record SignatureHelpInfo(IReadOnlyList<SignatureInfo> Signatures, int ActiveSignature, int ActiveParameter, int ArgumentListStart);
