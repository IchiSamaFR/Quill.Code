using Microsoft.CodeAnalysis;
using Quill.Code.Analysis;
using Quill.Code.CSharp;

namespace Quill.Code.Tests.CSharp;

public sealed class CSharpLanguageOptionsTests
{
    // Lignes « #! » neutralisées à longueur égale, comme les directives de packages de PocketForge.
    private static readonly CSharpLanguageService Transformed = new(() => CSharpTestProjects.ScriptOptions("Transformed") with
    {
        SourceTransform = code => code.Replace("#!", "//", StringComparison.Ordinal),
    });

    [Fact]
    public async Task SourceTransform_KeepsEditorPositions()
    {
        const string code = "#! directive\nConsole.Wri";
        var list = await Transformed.GetCompletionsAsync(code, code.Length);

        Assert.Contains(list.Items, i => i.Label == "WriteLine");
        Assert.Equal(code.Length - 3, list.SpanStart);
    }

    [Fact]
    public async Task SourceTransform_TransformedLinesAreNotCode()
    {
        var diagnostics = await Transformed.GetDiagnosticsAsync("#! not C#\nConsole.WriteLine();");

        Assert.DoesNotContain(diagnostics, d => d.Severity == CodeDiagnosticSeverity.Error);
    }

    [Fact]
    public async Task SourceTransform_MustKeepTheLength()
    {
        var service = new CSharpLanguageService(() => new CSharpLanguageOptions { SourceTransform = code => code[1..] });

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.GetDiagnosticsAsync("xy"));
    }

    [Fact]
    public async Task SemanticDiagnostics_CanBeDisabled()
    {
        var service = new CSharpLanguageService(() => new CSharpLanguageOptions { IncludeSemanticDiagnostics = false });

        Assert.Empty(await service.GetDiagnosticsAsync("class C { Unknown U; }"));
    }

    [Fact]
    public async Task Options_AreBuiltOnFirstRequest()
    {
        var service = new CSharpLanguageService(() => throw new NotSupportedException());

        await Assert.ThrowsAsync<NotSupportedException>(() => service.GetDiagnosticsAsync("x"));
    }

    [Fact]
    public void TextReferences_AddedOnlyWhenTheTextDeclaresThem()
    {
        var reference = MetadataReference.CreateFromFile(typeof(Uri).Assembly.Location);
        var service = new CSharpLanguageService(() => new CSharpLanguageOptions
        {
            Name = "TextReferences",
            TextReferences = code => code.Length > 0 ? new[] { reference } : Array.Empty<MetadataReference>(),
        });

        var first = service.Project.CreateDocument("a").Document.Project;
        var second = service.Project.CreateDocument("b").Document.Project;
        var without = service.Project.CreateDocument(string.Empty).Document.Project;

        Assert.Contains(reference, first.MetadataReferences);
        // Chaque document a son propre projet (WithText) : seules les references sont partagees.
        Assert.Same(first.MetadataReferences, second.MetadataReferences);
        Assert.DoesNotContain(reference, without.MetadataReferences);
    }

    [Fact]
    public async Task Project_CreatedFromOptions()
    {
        const string code = "Console.Wri";
        var list = await CSharpTestProjects.Script.GetCompletionsAsync(code, code.Length);

        Assert.Equal("Script", CSharpTestProjects.Script.Project.Project.Name);
        Assert.Contains(list.Items, i => i.Label == "WriteLine");
    }

    [Fact]
    public async Task DefaultOptions_ReferenceTheFramework()
    {
        var service = new CSharpLanguageService();

        var diagnostics = await service.GetDiagnosticsAsync("class C { System.Uri U => new(\"http://x\"); }");

        Assert.DoesNotContain(diagnostics, d => d.Severity == CodeDiagnosticSeverity.Error);
    }
}
