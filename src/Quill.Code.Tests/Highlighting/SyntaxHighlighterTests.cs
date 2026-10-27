using Quill.Code.Highlighting;

namespace Quill.Code.Tests.Highlighting;

public sealed class SyntaxHighlighterTests
{
    private static SyntaxDefinition Definition => SampleXshd.Definition;

    [Fact]
    public void Line_ColorsKeywordsStringsAndNumbers()
    {
        var tokens = SyntaxHighlighter.HighlightLine(Definition, "return \"a\" 42;", SyntaxLineState.Initial, out var end);

        Assert.Equal(
            new[] { (0, 6, "Keyword"), (7, 3, "String"), (11, 2, "Number") },
            tokens.Select(t => (t.Start, t.Length, t.Color?.Name)));
        Assert.False(end.IsInSpan);
    }

    [Fact]
    public void Keywords_LongestWordWins()
    {
        var token = Assert.Single(SyntaxHighlighter.HighlightLine(Definition, "double", SyntaxLineState.Initial, out _));

        Assert.Equal(6, token.Length);
    }

    [Fact]
    public void Keywords_NotInsideIdentifiers()
    {
        Assert.Empty(SyntaxHighlighter.HighlightLine(Definition, "redo doubles", SyntaxLineState.Initial, out _));
    }

    [Fact]
    public void LineComment_RunsToEndOfLine()
    {
        var token = Assert.Single(SyntaxHighlighter.HighlightLine(Definition, "// return 1", SyntaxLineState.Initial, out var end));

        Assert.Equal((0, 11, "Comment"), (token.Start, token.Length, token.Color?.Name));
        Assert.False(end.IsInSpan);
    }

    [Fact]
    public void MultilineSpan_StateCarriesToNextLines()
    {
        SyntaxHighlighter.HighlightLine(Definition, "1 /* a", SyntaxLineState.Initial, out var first);
        var middle = SyntaxHighlighter.HighlightLine(Definition, "return", first, out var second);
        var last = SyntaxHighlighter.HighlightLine(Definition, "b */ 2", second, out var third);

        Assert.True(first.IsInSpan);
        Assert.Equal((0, 6, "Comment"), (middle[0].Start, middle[0].Length, middle[0].Color?.Name));
        Assert.True(second.IsInSpan);
        Assert.Equal(new[] { (0, 4, "Comment"), (5, 1, "Number") }, last.Select(t => (t.Start, t.Length, t.Color?.Name)));
        Assert.False(third.IsInSpan);
    }

    [Fact]
    public void UnclosedString_ColorsToEndOfLineOnly()
    {
        var tokens = SyntaxHighlighter.HighlightLine(Definition, "\"abc", SyntaxLineState.Initial, out var end);

        Assert.Equal((0, 4, "String"), (tokens[0].Start, tokens[0].Length, tokens[0].Color?.Name));
        Assert.False(end.IsInSpan);
    }

    [Fact]
    public void Text_OffsetsAreRelativeToWholeText()
    {
        const string text = "/* a\r\nb */ return";
        var tokens = SyntaxHighlighter.Highlight(Definition, text);

        Assert.Equal(
            new[] { (0, 4, "Comment"), (6, 4, "Comment"), (11, 6, "Keyword") },
            tokens.Select(t => (t.Start, t.Length, t.Color?.Name)));
    }
}
