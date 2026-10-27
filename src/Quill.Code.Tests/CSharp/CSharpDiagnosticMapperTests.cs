using Quill.Code.CSharp.Analysis;

namespace Quill.Code.Tests.CSharp;

public sealed class CSharpDiagnosticMapperTests
{
    [Fact]
    public async Task MapAsync_BringsCompilationErrorsBackToEditorText()
    {
        const string text = "int x = \"a\";";
        var document = CSharpTestProjects.Wrap(text);
        var compilation = await document.Document.Project.GetCompilationAsync();

        var diagnostics = await CSharpDiagnosticMapper.MapAsync(document, compilation!.GetDiagnostics());

        var error = Assert.Single(diagnostics, d => d.Code == "CS0029");
        Assert.Equal(text.IndexOf('"', StringComparison.Ordinal), error.Start);
        Assert.Equal(3, error.Length);
    }
}
