using Microsoft.CodeAnalysis;

namespace Quill.Code.CSharp.Roslyn;

/// <summary>
/// Editor text placed in a Roslyn document. The source may differ from it (<see cref="CSharpLanguageOptions.SourceTransform"/>)
/// but keeps its length: text and source positions are the same.
/// </summary>
internal sealed class CSharpDocument(Document document, string text, CSharpLanguageOptions options)
{
    /// <summary>Analyzed Roslyn document.</summary>
    public Document Document { get; } = document;

    /// <summary>Editor text.</summary>
    public string Text { get; } = text;

    /// <summary>Options of the document's project.</summary>
    public CSharpLanguageOptions Options { get; } = options;

    /// <summary>Position clamped to the text.</summary>
    public int Clamp(int position) => Math.Clamp(position, 0, Text.Length);
}
