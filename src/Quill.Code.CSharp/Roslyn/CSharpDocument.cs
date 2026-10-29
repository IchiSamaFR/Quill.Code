using Microsoft.CodeAnalysis;

namespace Quill.Code.CSharp.Roslyn;

/// <summary>
/// Texte de l'éditeur placé dans un document Roslyn. Le source peut en différer (<see cref="CSharpLanguageOptions.SourceTransform"/>)
/// mais garde sa longueur : les positions du texte et du source sont les mêmes.
/// </summary>
internal sealed class CSharpDocument(Document document, string text, CSharpLanguageOptions options)
{
    /// <summary>Document Roslyn analysé.</summary>
    public Document Document { get; } = document;

    /// <summary>Texte de l'éditeur.</summary>
    public string Text { get; } = text;

    /// <summary>Options du projet du document.</summary>
    public CSharpLanguageOptions Options { get; } = options;

    /// <summary>Position bornée au texte.</summary>
    public int Clamp(int position) => Math.Clamp(position, 0, Text.Length);
}
