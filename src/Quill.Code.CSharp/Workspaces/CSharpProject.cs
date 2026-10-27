using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Quill.Code.CSharp.Workspaces;

/// <summary>
/// Projet Roslyn d'un éditeur et son document. Créé par Quill à la première demande (composition MEF, lecture des
/// références) ; chaque document est ensuite un instantané : le workspace n'est jamais modifié.
/// </summary>
public sealed class CSharpProject
{
    private readonly Lazy<(Project Project, DocumentId DocumentId, CSharpProjectOptions Options)> _state;
    private readonly object _gate = new();

    // Dernières références propres au texte : Roslyn réutilise sa compilation tant qu'elles ne changent pas.
    private (MetadataReference[] References, Project Project)? _textProject;

    internal CSharpProject(Func<(Project Project, DocumentId DocumentId, CSharpProjectOptions Options)> create)
    {
        _state = new(create);
    }

    /// <summary>Projet Roslyn (créé au premier accès).</summary>
    public Project Project => _state.Value.Project;

    /// <summary>
    /// Document du texte de l'éditeur, transformé par <see cref="CSharpProjectOptions.SourceTransform"/> et
    /// complété des <see cref="CSharpProjectOptions.TextReferences"/>.
    /// </summary>
    public CSharpDocument CreateDocument(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        var (project, documentId, options) = _state.Value;
        var source = options.SourceTransform?.Invoke(text) ?? text;
        if (source.Length != text.Length)
            throw new InvalidOperationException("SourceTransform doit conserver la longueur du texte.");

        return Create(WithTextReferences(project, options, text), documentId, source, text, 0);
    }

    /// <summary>Document dont le source contient le texte de l'éditeur à la position <paramref name="textStart"/>.</summary>
    /// <param name="source">Source complet analysé par Roslyn.</param>
    /// <param name="text">Texte de l'éditeur.</param>
    /// <param name="textStart">Position de <paramref name="text"/> dans <paramref name="source"/>.</param>
    public CSharpDocument CreateDocument(string source, string text, int textStart)
    {
        var (project, documentId, _) = _state.Value;
        return Create(project, documentId, source, text, textStart);
    }

    private Project WithTextReferences(Project project, CSharpProjectOptions options, string text)
    {
        if (options.TextReferences?.Invoke(text).Distinct().ToArray() is not { Length: > 0 } references)
            return project;

        lock (_gate)
        {
            if (_textProject is { } last && last.References.SequenceEqual(references))
                return last.Project;

            var withReferences = project.AddMetadataReferences(references);
            _textProject = (references, withReferences);
            return withReferences;
        }
    }

    private static CSharpDocument Create(Project project, DocumentId documentId, string source, string text, int textStart)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(text);
        if (textStart < 0 || textStart + text.Length > source.Length)
            throw new ArgumentOutOfRangeException(nameof(textStart), "Le texte de l'éditeur doit être contenu dans le source.");

        var document = project.GetDocument(documentId)
            ?? throw new InvalidOperationException("Le document de l'éditeur est introuvable dans le projet.");

        return new CSharpDocument(document.WithText(SourceText.From(source)), text, textStart);
    }
}
