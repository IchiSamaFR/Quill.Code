using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;
using System.Reflection;
using RoslynCompletion = Microsoft.CodeAnalysis.Completion;

namespace Quill.Code.CSharp.Workspaces;

/// <summary>
/// Workspace Roslyn partagé par l'application : la composition MEF (une à deux secondes) n'est faite qu'une fois.
/// Un projet par contexte (références, options) ; les documents sont des instantanés.
/// </summary>
public sealed class CSharpWorkspace : IDisposable
{
    // Services de complétion, survol et signatures : dans les assemblies Features, absentes des valeurs par défaut selon l'hôte.
    private static readonly Lazy<MefHostServices> Host = new(() => MefHostServices.Create(
        MefHostServices.DefaultAssemblies
            .Concat([typeof(RoslynCompletion.CompletionService).Assembly, Assembly.Load("Microsoft.CodeAnalysis.CSharp.Features")])
            .Distinct()));

    private readonly AdhocWorkspace _workspace = new(Host.Value);
    private readonly object _gate = new();

    /// <summary>Ajoute un projet et son document d'éditeur (vide).</summary>
    public CSharpProject AddProject(CSharpProjectOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        lock (_gate)
        {
            var info = ProjectInfo.Create(
                ProjectId.CreateNewId(),
                VersionStamp.Create(),
                options.Name,
                options.Name,
                LanguageNames.CSharp,
                compilationOptions: options.CompilationOptions,
                parseOptions: options.ParseOptions,
                metadataReferences: options.References);

            var projectId = _workspace.AddProject(info).Id;
            foreach (var (name, source) in options.AdditionalDocuments)
                _workspace.AddDocument(projectId, name, SourceText.From(source));

            var document = _workspace.AddDocument(projectId, options.DocumentName, SourceText.From(string.Empty));
            return new CSharpProject(document.Project, document.Id);
        }
    }

    /// <inheritdoc />
    public void Dispose() => _workspace.Dispose();
}
