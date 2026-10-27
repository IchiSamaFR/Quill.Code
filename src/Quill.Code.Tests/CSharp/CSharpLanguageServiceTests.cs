using Quill.Code.Analysis;
using Quill.Code.Completion;
using Quill.Code.CSharp;

namespace Quill.Code.Tests.CSharp;

public sealed class CSharpLanguageServiceTests
{
    private readonly CSharpLanguageService _service = new();

    [Fact]
    public async Task Completion_ProposesMembers()
    {
        const string text = "Console.Wri";
        var list = await _service.GetCompletionsAsync(CSharpTestProjects.Script.CreateDocument(text), text.Length);

        var writeLine = Assert.Single(list.Items, i => i.Label == "WriteLine");
        Assert.Equal(CompletionItemKind.Method, writeLine.Kind);
        Assert.Equal(8, list.SpanStart);
        Assert.Equal(3, list.SpanLength);
    }

    [Fact]
    public async Task Completion_PositionsAreRelativeToEditorText()
    {
        const string text = "var s = \"\"; s.Len";
        var list = await _service.GetCompletionsAsync(CSharpTestProjects.Wrap(text), text.Length);

        Assert.Contains(list.Items, i => i.Label == "Length" && i.Kind == CompletionItemKind.Property);
        Assert.Equal(text.Length - 3, list.SpanStart);
    }

    [Fact]
    public async Task Change_ReplacesTypedWord()
    {
        const string text = "Console.Wri";
        var document = CSharpTestProjects.Script.CreateDocument(text);
        var list = await _service.GetCompletionsAsync(document, text.Length);

        var change = await _service.GetChangeAsync(document, list.Items.First(i => i.Label == "WriteLine"));

        Assert.Equal(8, change.SpanStart);
        Assert.Equal(3, change.SpanLength);
        Assert.Equal("WriteLine", change.NewText);
    }

    [Fact]
    public async Task Diagnostics_ReportSyntaxErrors()
    {
        var diagnostics = await _service.GetDiagnosticsAsync(CSharpTestProjects.Script.CreateDocument("int x = ;"));

        var error = Assert.Single(diagnostics, d => d.Code == "CS1525");
        Assert.Equal(CodeDiagnosticSeverity.Error, error.Severity);
        Assert.InRange(error.Start, 0, 8);
    }

    [Fact]
    public async Task Diagnostics_MissingTokenAfterTextIsAttachedToItsEnd()
    {
        const string text = "var x = 1";
        var diagnostics = await _service.GetDiagnosticsAsync(CSharpTestProjects.Wrap(text));

        var error = Assert.Single(diagnostics, d => d.Code == "CS1002");
        Assert.InRange(error.Start, 0, text.Length - 1);
        Assert.Equal(1, error.Length);
    }

    [Fact]
    public async Task Diagnostics_SemanticCanBeDisabled()
    {
        var document = CSharpTestProjects.Wrap("unknown.Foo();") with { IncludeSemanticDiagnostics = false };

        Assert.Empty(await _service.GetDiagnosticsAsync(document));
    }

    [Fact]
    public async Task Hover_DescribesSymbol()
    {
        var hover = await _service.GetHoverAsync(CSharpTestProjects.Wrap("System.Console.WriteLine();"), 17);

        Assert.NotNull(hover);
        Assert.Equal(15, hover.Start);
        Assert.Equal("WriteLine".Length, hover.Length);
        Assert.Contains("WriteLine", hover.Signature, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SignatureHelp_TracksActiveParameter()
    {
        const string text = "System.Math.Max(1, ";
        var help = await _service.GetSignatureHelpAsync(CSharpTestProjects.Wrap(text), text.Length);

        Assert.NotNull(help);
        Assert.NotEmpty(help.Signatures);
        Assert.Equal(1, help.ActiveParameter);
        Assert.Equal(text.IndexOf('(', StringComparison.Ordinal), help.ArgumentListStart);
    }
}
