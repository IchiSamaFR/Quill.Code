using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;
using Quill.Code.CSharp.Compilation;
using System.Reflection;
using System.Text;
using RoslynCompletion = Microsoft.CodeAnalysis.Completion;

namespace Quill.Code.CSharp.Roslyn;

/// <summary>
/// Hôte Roslyn de l'application : la composition MEF (une à deux secondes) n'est faite qu'une fois, à la première demande.
/// Les projets sont des instantanés tirés de la solution vide : le workspace n'est jamais modifié, rien n'y reste.
/// </summary>
internal static class RoslynHost
{
    // Services de complétion, survol et signatures : dans les assemblies Features, absentes des valeurs par défaut selon l'hôte.
    private static readonly Lazy<AdhocWorkspace> Workspace = new(() => new AdhocWorkspace(MefHostServices.Create(
        MefHostServices.DefaultAssemblies
            .Concat([typeof(RoslynCompletion.CompletionService).Assembly, Assembly.Load("Microsoft.CodeAnalysis.CSharp.Features")])
            .Distinct())));

    /// <summary>Projet décrit par <paramref name="options"/> et document vide qui recevra le texte de l'éditeur.</summary>
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

        if (options.IncludeScriptRuntime)
            solution = AddDocument(solution, projectId, CSharpScriptRuntime.FileName, CSharpScriptRuntime.Source).Solution;

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
