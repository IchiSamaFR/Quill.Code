using Microsoft.CodeAnalysis;
using Quill.Code.Analysis;
using Quill.Code.CSharp;
using Quill.Code.CSharp.Workspaces;

namespace Quill.Code.Tests.CSharp;

public sealed class CSharpLanguageServiceExtensionsTests
{
    private readonly CSharpLanguageService _service = new();

    [Fact]
    public async Task Completions_FromCode_WithSourceTransform()
    {
        const string code = "#! directive\nConsole.Wri";
        var list = await _service.GetCompletionsAsync(CSharpTestProjects.Transformed, code, code.Length);

        Assert.Contains(list.Items, i => i.Label == "WriteLine");
        Assert.Equal(code.Length - 3, list.SpanStart);
    }

    [Fact]
    public async Task Diagnostics_TransformedLinesAreNotCode()
    {
        var diagnostics = await _service.GetDiagnosticsAsync(CSharpTestProjects.Transformed, "#! not C#\nConsole.WriteLine();");

        Assert.DoesNotContain(diagnostics, d => d.Severity == CodeDiagnosticSeverity.Error);
    }

    [Fact]
    public void SourceTransform_MustContainTheText()
    {
        var project = CSharpTestProjects.SharedWorkspace.AddProject(new CSharpProjectOptions
        {
            Name = "BadTransform",
            SourceTransform = code => code[1..],
        });

        Assert.Throws<InvalidOperationException>(() => project.CreateDocument("xy"));
    }

    [Fact]
    public async Task SourceTransform_WrapsTheTextInGeneratedCode()
    {
        const string prefix = "class Wrapper { int Run() { var value = 41; return ";
        using var service = new CSharpLanguageService("Wrapped", () => new CSharpProjectOptions
        {
            References = CSharpReferences.FromTrustedPlatformAssemblies(),
            SourceTransform = code => new CSharpSource(prefix + code + "; } }", prefix.Length),
            AllowImports = false,
        });

        const string code = "value.ToStr";
        var list = await service.GetCompletionsAsync(code, code.Length);
        var diagnostics = await service.GetDiagnosticsAsync("value + unknown");

        Assert.Contains(list.Items, i => i.Label == "ToString");
        Assert.Equal("value.".Length, list.SpanStart);
        Assert.Equal("unknown", "value + unknown".Substring(Assert.Single(diagnostics).Start, "unknown".Length));
    }

    [Fact]
    public async Task SemanticDiagnostics_CanBeDisabledByProject()
    {
        using var service = new CSharpLanguageService("SyntaxOnly", () => new CSharpProjectOptions { IncludeSemanticDiagnostics = false });

        Assert.Empty(await service.GetDiagnosticsAsync("class C { Unknown U; }"));
    }

    [Fact]
    public void AddProject_CreatesNothingUntilFirstUse()
    {
        var project = CSharpTestProjects.SharedWorkspace.AddProject(() => throw new NotSupportedException());

        Assert.Throws<NotSupportedException>(() => project.CreateDocument("x"));
    }

    [Fact]
    public void TextReferences_AddedOnlyWhenTheTextDeclaresThem()
    {
        var reference = MetadataReference.CreateFromFile(typeof(Uri).Assembly.Location);
        var project = CSharpTestProjects.SharedWorkspace.AddProject(new CSharpProjectOptions
        {
            Name = "TextReferences",
            TextReferences = code => code.Length > 0 ? new[] { reference } : Array.Empty<MetadataReference>(),
        });

        var first = project.CreateDocument("a").Document.Project;
        var second = project.CreateDocument("b").Document.Project;
        var without = project.CreateDocument(string.Empty).Document.Project;

        Assert.Contains(reference, first.MetadataReferences);
        Assert.Contains(reference, second.MetadataReferences);
        Assert.DoesNotContain(reference, without.MetadataReferences);
    }

    [Fact]
    public async Task OwnProject_CreatedFromNameAndOptions()
    {
        using var service = new CSharpLanguageService("Script", () => new CSharpProjectOptions
        {
            References = CSharpReferences.FromTrustedPlatformAssemblies(),
            CompilationOptions = new Microsoft.CodeAnalysis.CSharp.CSharpCompilationOptions(OutputKind.ConsoleApplication),
            AdditionalDocuments = new Dictionary<string, string> { ["GlobalUsings.cs"] = "global using System;" },
        });

        const string code = "Console.Wri";
        var list = await service.GetCompletionsAsync(code, code.Length);

        Assert.Equal("Script", service.Project.Project.Name);
        Assert.Contains(list.Items, i => i.Label == "WriteLine");
    }

    [Fact]
    public async Task DefaultProject_ReferencesTheFramework()
    {
        using var service = new CSharpLanguageService();

        var diagnostics = await service.GetDiagnosticsAsync("class C { System.Uri U => new(\"http://x\"); }");

        Assert.DoesNotContain(diagnostics, d => d.Severity == CodeDiagnosticSeverity.Error);
    }
}
