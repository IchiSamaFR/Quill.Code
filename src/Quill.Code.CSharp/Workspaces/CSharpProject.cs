using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Text;

namespace Quill.Code.CSharp.Workspaces;

/// <summary>
/// Projet Roslyn d'un éditeur et son document. Créé par Quill à la première demande (composition MEF, lecture des
/// références) ; chaque document est ensuite un instantané : le workspace n'est jamais modifié.
/// Des sources peuvent être ajoutées, remplacées ou retirées à la volée (ex. classe utilitaire lue d'un fichier .cs) :
/// le texte de l'éditeur les voit (complétion, analyse) et elles sont compilées avec lui.
/// </summary>
public sealed class CSharpProject
{
    private readonly Lazy<(Project Project, DocumentId DocumentId, CSharpProjectOptions Options)> _state;
    private readonly object _gate = new();

    // Sources ajoutées à la volée (clé → document) ; appliquées au projet à sa prochaine utilisation.
    private readonly Dictionary<string, (string Name, string? FilePath, SourceText Text)> _sources = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, DocumentId> _sourceIds = new(StringComparer.OrdinalIgnoreCase);
    private Project? _project;
    private bool _sourcesChanged;

    // Dernières références propres au texte : Roslyn réutilise sa compilation tant qu'elles ne changent pas.
    private (Project Base, MetadataReference[] References, Project Project)? _textProject;

    internal CSharpProject(Func<(Project Project, DocumentId DocumentId, CSharpProjectOptions Options)> create)
    {
        _state = new(create);
    }

    /// <summary>Levé quand une source est ajoutée, remplacée ou retirée (ex. pour relancer l'analyse de l'éditeur).</summary>
    public event EventHandler? SourcesChanged;

    /// <summary>Projet Roslyn (créé au premier accès), sources ajoutées à la volée comprises.</summary>
    public Project Project => Current().Project;

    /// <summary>Clés des sources ajoutées à la volée.</summary>
    public IReadOnlyCollection<string> Sources
    {
        get
        {
            lock (_gate)
                return [.. _sources.Keys];
        }
    }

    /// <summary>Ajoute ou remplace une source (ex. une classe utilitaire), identifiée par son nom.</summary>
    /// <param name="name">Nom du document (ex. <c>FileHelper.cs</c>), repris dans les erreurs de compilation.</param>
    /// <param name="code">Code C# de la source.</param>
    public void SetSource(string name, string code)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        SetSource(name, name, null, code);
    }

    /// <summary>Lit un fichier .cs et l'ajoute (ou le remplace) ; il est identifié par son chemin complet.</summary>
    /// <returns>Clé de la source, à passer à <see cref="RemoveSource"/>.</returns>
    public async Task<string> SetSourceFileAsync(string path, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var fullPath = Path.GetFullPath(path);
        var code = await File.ReadAllTextAsync(fullPath, cancellationToken).ConfigureAwait(false);
        SetSource(fullPath, Path.GetFileName(fullPath), fullPath, code);
        return fullPath;
    }

    /// <summary>Retire une source ajoutée à la volée (nom ou chemin complet).</summary>
    /// <returns>Faux si aucune source ne porte cette clé.</returns>
    public bool RemoveSource(string key)
    {
        ArgumentNullException.ThrowIfNull(key);

        lock (_gate)
        {
            if (!_sources.Remove(key))
                return false;

            _sourcesChanged = true;
        }

        SourcesChanged?.Invoke(this, EventArgs.Empty);
        return true;
    }

    /// <summary>Retire toutes les sources ajoutées à la volée.</summary>
    public void ClearSources()
    {
        lock (_gate)
        {
            if (_sources.Count == 0)
                return;

            _sources.Clear();
            _sourcesChanged = true;
        }

        SourcesChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Document du texte de l'éditeur, placé par <see cref="CSharpProjectOptions.SourceTransform"/>, complété des
    /// <see cref="CSharpProjectOptions.TextReferences"/> et réglé selon les options du projet.
    /// </summary>
    public CSharpDocument CreateDocument(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        var (project, documentId, options) = Current();
        var (source, textStart) = options.SourceTransform?.Invoke(text) ?? text;
        if (source is null || textStart < 0 || textStart + text.Length > source.Length)
            throw new InvalidOperationException("SourceTransform doit rendre un source qui contient le texte (même longueur) à TextStart.");

        return Create(WithTextReferences(project, options, text), documentId, source, text, textStart) with
        {
            IncludeSemanticDiagnostics = options.IncludeSemanticDiagnostics,
            AllowImports = options.AllowImports,
        };
    }

    /// <summary>Document dont le source contient le texte de l'éditeur à la position <paramref name="textStart"/>.</summary>
    /// <param name="source">Source complet analysé par Roslyn.</param>
    /// <param name="text">Texte de l'éditeur.</param>
    /// <param name="textStart">Position de <paramref name="text"/> dans <paramref name="source"/>.</param>
    public CSharpDocument CreateDocument(string source, string text, int textStart)
    {
        var (project, documentId, _) = Current();
        return Create(project, documentId, source, text, textStart);
    }

    private void SetSource(string key, string name, string? filePath, string code)
    {
        ArgumentNullException.ThrowIfNull(code);

        lock (_gate)
        {
            // Encodage requis pour émettre les informations de débogage (PDB).
            _sources[key] = (name, filePath, SourceText.From(code, Encoding.UTF8));
            _sourcesChanged = true;
        }

        SourcesChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Projet à jour des sources ajoutées à la volée (rien n'est créé avant la première utilisation).</summary>
    private (Project Project, DocumentId DocumentId, CSharpProjectOptions Options) Current()
    {
        var (initial, documentId, options) = _state.Value;
        lock (_gate)
        {
            _project ??= initial;
            if (!_sourcesChanged)
                return (_project, documentId, options);

            var project = _project;
            foreach (var key in _sourceIds.Keys.Where(k => !_sources.ContainsKey(k)).ToArray())
            {
                project = project.RemoveDocument(_sourceIds[key]);
                _sourceIds.Remove(key);
            }

            foreach (var (key, (name, filePath, text)) in _sources)
            {
                if (_sourceIds.TryGetValue(key, out var id))
                {
                    if (project.GetDocument(id) is { } existing && existing.TryGetText(out var current) && current == text)
                        continue;

                    project = project.Solution.WithDocumentText(id, text).GetProject(project.Id)!;
                }
                else
                {
                    var document = project.AddDocument(name, text, filePath: filePath);
                    _sourceIds[key] = document.Id;
                    project = document.Project;
                }
            }

            _project = project;
            _sourcesChanged = false;
            return (project, documentId, options);
        }
    }

    private Project WithTextReferences(Project project, CSharpProjectOptions options, string text)
    {
        if (options.TextReferences?.Invoke(text).Distinct().ToArray() is not { Length: > 0 } references)
            return project;

        lock (_gate)
        {
            if (_textProject is { } last && ReferenceEquals(last.Base, project) && last.References.SequenceEqual(references))
                return last.Project;

            var withReferences = project.AddMetadataReferences(references);
            _textProject = (project, references, withReferences);
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

        // Encodage requis pour émettre les informations de débogage (PDB).
        return new CSharpDocument(document.WithText(SourceText.From(source, Encoding.UTF8)), text, textStart);
    }
}
