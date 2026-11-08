using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;
using Quill.Code.CSharp.Compilation;
using System.Reflection;
using System.Text;
using RoslynCompletion = Microsoft.CodeAnalysis.Completion;
using Quill.Code.CSharp.Constants;

namespace Quill.Code.CSharp.Roslyn;

/// <summary>
/// Roslyn host of the application: MEF composition (one to two seconds) is done only once, on first request.
/// Projects are snapshots taken from the empty solution: the workspace is never modified, nothing stays in it.
/// </summary>
internal static class RoslynHost
{
    // Services de complétion, survol et signatures : dans les assemblies Features, absentes des valeurs par défaut selon l'hôte.
    private static readonly Lazy<AdhocWorkspace> Workspace = new(() => new AdhocWorkspace(MefHostServices.Create(
        MefHostServices.DefaultAssemblies
            .Concat([typeof(RoslynCompletion.CompletionService).Assembly, Assembly.Load("Microsoft.CodeAnalysis.CSharp.Features")])
            .Distinct())));

    /// <summary>Project described by <paramref name="options"/> and empty document that will receive the editor text.</summary>
    public static (Project Project, DocumentId DocumentId) CreateProject(CSharpLanguageOptions options)
    {
        var projectId = ProjectId.CreateNewId(options.Name);
        var solution = Workspace.Value.CurrentSolution.AddProject(ProjectInfo.Create(
            projectId,
            VersionStamp.Create(),
            options.Name,
            options.Name,
            LanguageNames.CSharp,
            compilationOptions: options.CompilationOptions,
            parseOptions: options.ParseOptions,
            metadataReferences: options.References));

        foreach (var (name, source) in options.AdditionalDocuments)
            solution = AddDocument(solution, projectId, name, source).Solution;

        if (options.IncludeLogRuntime)
            solution = AddDocument(solution, projectId, CSharpScriptConstants.FileName, CSharpScriptRuntime.Source).Solution;

        var (withDocument, documentId) = AddDocument(solution, projectId, options.DocumentName, string.Empty);
        return (withDocument.GetProject(projectId)!, documentId);
    }

    // Encodage requis pour émettre les informations de débogage (PDB).
    private static (Solution Solution, DocumentId Id) AddDocument(Solution solution, ProjectId projectId, string name, string source)
    {
        var id = DocumentId.CreateNewId(projectId, name);
        return (solution.AddDocument(id, name, SourceText.From(source, Encoding.UTF8)), id);
    }
}
