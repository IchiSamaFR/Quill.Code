using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Text;

namespace Quill.Code.CSharp.Roslyn;

/// <summary>
/// Roslyn project of a <see cref="CSharpLanguageService"/>, created on first request; each document is then a
/// snapshot. Sources added on the fly are applied to the project on its next use.
/// </summary>
internal sealed class CSharpProject
{
    private readonly Lazy<(Project Project, DocumentId DocumentId, CSharpLanguageOptions Options)> _state;
    private readonly object _gate = new();

    private readonly Dictionary<string, (string Name, string? FilePath, SourceText Text)> _sources = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, DocumentId> _sourceIds = new(StringComparer.OrdinalIgnoreCase);
    private Project? _project;
    private bool _sourcesChanged;

    // Dernières références propres au texte : Roslyn réutilise sa compilation tant qu'elles ne changent pas.
    private (Project Base, MetadataReference[] References, Project Project)? _textProject;

    public CSharpProject(Func<CSharpLanguageOptions> options)
    {
        _state = new(() =>
        {
            var created = options();
            var (project, documentId) = RoslynHost.CreateProject(created);
            return (project, documentId, created);
        });
    }

    public event EventHandler? SourcesChanged;

    /// <summary>Roslyn project (created on first access), including sources added on the fly.</summary>
    public Project Project => Current().Project;

    public IReadOnlyCollection<string> Sources
    {
        get
        {
            lock (_gate)
                return [.. _sources.Keys];
        }
    }

    public void SetSource(string name, string code)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        SetSource(name, name, null, code);
    }

    public async Task<string> SetSourceFileAsync(string path, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var fullPath = Path.GetFullPath(path);
        var code = await File.ReadAllTextAsync(fullPath, cancellationToken).ConfigureAwait(false);
        SetSource(fullPath, Path.GetFileName(fullPath), fullPath, code);
        return fullPath;
    }

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

    /// <summary>Document of the editor text, transformed and completed with its own references.</summary>
    public CSharpDocument CreateDocument(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        var (project, documentId, options) = Current();
        var source = options.SourceTransform?.Invoke(text) ?? text;
        if (source.Length != text.Length)
            throw new InvalidOperationException("SourceTransform doit rendre un source de même longueur que le texte.");

        var document = WithTextReferences(project, options, text).GetDocument(documentId)
            ?? throw new InvalidOperationException("Le document de l'éditeur est introuvable dans le projet.");

        // Encodage requis pour émettre les informations de débogage (PDB).
        return new CSharpDocument(document.WithText(SourceText.From(source, Encoding.UTF8)), text, options);
    }

    private void SetSource(string key, string name, string? filePath, string code)
    {
        ArgumentNullException.ThrowIfNull(code);

        lock (_gate)
        {
            _sources[key] = (name, filePath, SourceText.From(code, Encoding.UTF8));
            _sourcesChanged = true;
        }

        SourcesChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Project updated with the sources added on the fly.</summary>
    private (Project Project, DocumentId DocumentId, CSharpLanguageOptions Options) Current()
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

    private Project WithTextReferences(Project project, CSharpLanguageOptions options, string text)
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
}
