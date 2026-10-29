using Quill.Code.Analysis;
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
    public async Task OtherServices_ProposeNothing()
    {
        Assert.Empty((await _service.GetCompletionsAsync("SELECT ", 7)).Items);
        Assert.Null(await _service.GetHoverAsync("SELECT 1", 2));
        Assert.Null(await _service.GetSignatureHelpAsync("SELECT 1", 2));
    }

    [Fact]
    public async Task HostServices_ReplaceTheDefaults()
    {
        var hover = new FixedHover(new HoverInfo(0, 6, "SELECT"));
        var service = new SqlLanguageService(hover: hover);

        Assert.Same(hover.Info, await service.GetHoverAsync("SELECT 1", 2));
        Assert.NotEmpty(await service.GetDiagnosticsAsync("SELECT * FORM t"));
    }

    private sealed class FixedHover(HoverInfo info) : ICodeHover
    {
        public HoverInfo Info { get; } = info;

        public Task<HoverInfo?> GetHoverAsync(string code, int position, CancellationToken cancellationToken = default)
            => Task.FromResult<HoverInfo?>(Info);
    }
}
