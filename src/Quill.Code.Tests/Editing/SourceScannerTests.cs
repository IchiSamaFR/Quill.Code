using Quill.Code.Editing;

namespace Quill.Code.Tests.Editing;

public sealed class SourceScannerTests
{
    [Theory]
    [InlineData("a // (", 5, SourceRegion.LineComment)]
    [InlineData("a // (\nb", 8, SourceRegion.Code)]
    [InlineData("/* x */ y", 8, SourceRegion.Code)]
    [InlineData("/* x ", 5, SourceRegion.BlockComment)]
    [InlineData("\"a\\\" b", 6, SourceRegion.String)]
    [InlineData("@\"a\"\" b", 7, SourceRegion.String)]
    [InlineData("$@\"a\\\" b", 8, SourceRegion.Code)]
    [InlineData("\"\"\"a \" b", 8, SourceRegion.String)]
    [InlineData("\"\"\"a\"\"\" b", 9, SourceRegion.Code)]
    [InlineData("\"abc\nx", 6, SourceRegion.Code)]
    public void CSharp(string text, int offset, SourceRegion expected)
        => Assert.Equal(expected, SourceScanner.RegionAt(text, offset, SourceSyntax.CSharp));

    [Theory]
    [InlineData("select 'it''s ", 14, SourceRegion.String)]
    [InlineData("select 'a' -- x", 15, SourceRegion.LineComment)]
    [InlineData("select 'a\\' b", 13, SourceRegion.Code)]
    public void Sql(string text, int offset, SourceRegion expected)
        => Assert.Equal(expected, SourceScanner.RegionAt(text, offset, SourceSyntax.Sql));
}
