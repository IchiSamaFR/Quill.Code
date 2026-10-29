using Quill.Code.Languages;
using Quill.Code.Sql;

namespace Quill.Code.Tests.Sql;

public class SqlLanguageServiceTests
{
    private readonly SqlLanguageService _service = new();

    [Fact]
    public void Language_IsSql()
        => Assert.Equal(CodeLanguages.Sql, _service.Language);

    [Fact]
    public async Task Diagnostics_ReportSyntaxErrors()
    {
        Assert.Empty(await _service.GetDiagnosticsAsync("SELECT 1"));
        Assert.NotEmpty(await _service.GetDiagnosticsAsync("SELECT * FORM t"));
    }

    [Fact]
    public async Task SignatureHelp_ProposesNothing()
        => Assert.Null(await _service.GetSignatureHelpAsync("SELECT 1", 2));

    [Fact]
    public async Task Change_ReplacesTheLastWord()
    {
        var list = await _service.GetCompletionsAsync("SELECT * FR", 11);

        var change = await _service.GetChangeAsync("SELECT * FR", list.Items.First(i => i.Label == "FROM"));

        Assert.Equal((9, 2, "FROM"), (change.SpanStart, change.SpanLength, change.NewText));
    }
}
