using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Quill.Code.CSharp.Workspaces;

/// <summary>
/// Instantané d'un projet Roslyn et de son document d'éditeur. Immuable : créer un document ou changer
/// les références ne modifie jamais le workspace, et Roslyn réutilise sa compilation tant que les références sont les mêmes.
/// </summary>
public sealed class CSharpProject
{
    private readonly DocumentId _documentId;

    internal CSharpProject(Project project, DocumentId documentId)
    {
        Project = project;
        _documentId = documentId;
    }

    /// <summary>Projet Roslyn.</summary>
    public Project Project { get; }

    /// <summary>Même projet avec d'autres références (ex. assemblies des packages d'un script).</summary>
    public CSharpProject WithReferences(IEnumerable<MetadataReference> references)
        => new(Project.WithMetadataReferences(references), _documentId);

    /// <summary>Document dont le source est exactement le texte de l'éditeur.</summary>
    public CSharpDocument CreateDocument(string text) => CreateDocument(text, text, 0);

    /// <summary>Document dont le source contient le texte de l'éditeur à la position <paramref name="textStart"/>.</summary>
    /// <param name="source">Source complet analysé par Roslyn.</param>
    /// <param name="text">Texte de l'éditeur.</param>
    /// <param name="textStart">Position de <paramref name="text"/> dans <paramref name="source"/>.</param>
    public CSharpDocument CreateDocument(string source, string text, int textStart)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(text);
        if (textStart < 0 || textStart + text.Length > source.Length)
            throw new ArgumentOutOfRangeException(nameof(textStart), "Le texte de l'éditeur doit être contenu dans le source.");

        var document = Project.GetDocument(_documentId)
            ?? throw new InvalidOperationException("Le document de l'éditeur est introuvable dans le projet.");

        return new CSharpDocument(document.WithText(SourceText.From(source)), text, textStart);
    }
}
