using Quill.Code.CSharp.Features;

namespace Quill.Code.Tests.CSharp;

public sealed class CSharpDiagnosticMapperTests
{
    [Fact]
    public async Task MapAsync_KeepsOnlyEditorTextDiagnostics()
    {
        const string text = "int x = \"a\";";
        var document = CSharpTestProjects.Script.Project.CreateDocument(text);
        var compilation = await document.Document.Project.GetCompilationAsync();

        var diagnostics = await CSharpDiagnosticMapper.MapAsync(document, compilation!.GetDiagnostics());

        var error = Assert.Single(diagnostics, d => d.Code == "CS0029");
        Assert.Equal(text.IndexOf('"', StringComparison.Ordinal), error.Start);
        Assert.Equal(3, error.Length);
    }
}
