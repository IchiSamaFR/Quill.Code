using Quill.Code.Editing;

namespace Quill.Code.Tests.Editing;

public sealed class BracketMatcherTests
{
    [Theory]
    [InlineData("f(a(b)c)", 2, 1, 7)]
    [InlineData("f(a(b)c)", 8, 1, 7)]
    [InlineData("f(\")\")", 1, 1, 5)]
    [InlineData("f( // )\n)", 2, 1, 8)]
    public void FindPair_IgnoresStringsAndComments(string text, int caret, int open, int close)
        => Assert.Equal(new BracketPair(open, close), BracketMatcher.FindPair(text, caret, SourceSyntax.CSharp));

    [Fact]
    public void FindPair_PrefersClosingBeforeCaret()
        => Assert.Equal(new BracketPair(0, 1), BracketMatcher.FindPair("()()", 2, SourceSyntax.CSharp));

    [Fact]
    public void FindPair_NothingWhenUnbalanced()
        => Assert.Null(BracketMatcher.FindPair("(a", 0, SourceSyntax.CSharp));
}
