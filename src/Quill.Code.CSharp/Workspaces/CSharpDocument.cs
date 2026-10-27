using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Quill.Code.CSharp.Workspaces;

/// <summary>
/// Texte de l'éditeur placé dans un document Roslyn. Le source du document peut l'envelopper (contexte généré)
/// ou le neutraliser à longueur égale (directives) : les positions échangées avec Quill sont toujours celles de <see cref="Text"/>.
/// </summary>
public sealed record CSharpDocument
{
    /// <param name="document">Document Roslyn dont le source contient le texte de l'éditeur.</param>
    /// <param name="text">Texte de l'éditeur, tel que l'utilisateur le voit.</param>
    /// <param name="textStart">Position du texte de l'éditeur dans le source du document.</param>
    public CSharpDocument(Document document, string text, int textStart = 0)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(text);
        ArgumentOutOfRangeException.ThrowIfNegative(textStart);

        Document = document;
        Text = text;
        TextStart = textStart;
    }

    /// <summary>Document Roslyn analysé.</summary>
    public Document Document { get; }

    /// <summary>Texte de l'éditeur.</summary>
    public string Text { get; }

    /// <summary>Position de <see cref="Text"/> dans le source du document.</summary>
    public int TextStart { get; }

    /// <summary>Fin de <see cref="Text"/> dans le source du document.</summary>
    public int TextEnd => TextStart + Text.Length;

    /// <summary>
    /// Faux pour ne signaler que les fautes de syntaxe, quand le contexte manque pour résoudre les noms
    /// (ex. requête sans DbContext).
    /// </summary>
    public bool IncludeSemanticDiagnostics { get; init; } = true;

    /// <summary>
    /// Faux quand le texte ne peut pas recevoir de « using » : un type d'un espace de noms non importé est alors
    /// inséré avec son nom complet, et une méthode d'extension non importée n'est pas proposée.
    /// </summary>
    public bool AllowImports { get; init; } = true;

    /// <summary>Vrai si <paramref name="span"/> (position du source) est entièrement dans le texte de l'éditeur.</summary>
    public bool Contains(TextSpan span) => span.Start >= TextStart && span.End <= TextEnd;

    /// <summary>Position du texte de l'éditeur ramenée dans le source (bornée au texte).</summary>
    internal int ToSource(int position) => TextStart + Math.Clamp(position, 0, Text.Length);

    /// <summary>Position du source ramenée dans le texte de l'éditeur (peut sortir du texte).</summary>
    internal int ToText(int sourcePosition) => sourcePosition - TextStart;
}
