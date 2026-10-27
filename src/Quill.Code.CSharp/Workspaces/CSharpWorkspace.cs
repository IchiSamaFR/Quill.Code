using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;
using System.Reflection;
using System.Text;
using RoslynCompletion = Microsoft.CodeAnalysis.Completion;

namespace Quill.Code.CSharp.Workspaces;

/// <summary>
/// Workspace Roslyn partagé par l'application : la composition MEF (une à deux secondes) n'est faite qu'une fois,
/// à la première demande. Un projet par contexte (références, options) ; les documents sont des instantanés.
/// </summary>
public sealed class CSharpWorkspace : IDisposable
{
    // Services de complétion, survol et signatures : dans les assemblies Features, absentes des valeurs par défaut selon l'hôte.
    private static readonly Lazy<MefHostServices> Host = new(() => MefHostServices.Create(
        MefHostServices.DefaultAssemblies
            .Concat([typeof(RoslynCompletion.CompletionService).Assembly, Assembly.Load("Microsoft.CodeAnalysis.CSharp.Features")])
            .Distinct()));

    private readonly Lazy<AdhocWorkspace> _workspace = new(() => new AdhocWorkspace(Host.Value));
    private readonly object _gate = new();

    /// <summary>Déclare un projet : il n'est créé qu'à sa première utilisation.</summary>
    public CSharpProject AddProject(CSharpProjectOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        return AddProject(() => options);
    }

    /// <summary>
    /// Déclare un projet dont les options ne sont construites qu'à sa première utilisation
    /// (ex. références lues sur le disque) : rien n'est fait à l'appel, donc rien sur le thread de l'interface.
    /// </summary>
    public CSharpProject AddProject(Func<CSharpProjectOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        return new CSharpProject(() => Create(options()));
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_workspace.IsValueCreated)
            _workspace.Value.Dispose();
    }

    private (Project Project, DocumentId DocumentId, CSharpProjectOptions Options) Create(CSharpProjectOptions options)
    {
        lock (_gate)
        {
            var workspace = _workspace.Value;
            var info = ProjectInfo.Create(
                ProjectId.CreateNewId(),
                VersionStamp.Create(),
                options.Name,
                options.Name,
                LanguageNames.CSharp,
                compilationOptions: options.CompilationOptions,
                parseOptions: options.ParseOptions,
                metadataReferences: options.References);

            var projectId = workspace.AddProject(info).Id;
            foreach (var (name, source) in options.AdditionalDocuments)
                workspace.AddDocument(projectId, name, SourceText.From(source, Encoding.UTF8));

            var document = workspace.AddDocument(projectId, options.DocumentName, SourceText.From(string.Empty, Encoding.UTF8));
            return (document.Project, document.Id, options);
        }
    }
}
