using Quill.Code.Editing;

namespace Quill.Code.Tests.Editing;

public sealed class CodeEditingTests
{
    private static readonly CodeEditingOptions CSharp = new();
    private static readonly CodeEditingOptions CSharpCrLf = new() { NewLine = "\r\n" };
    private static readonly CodeEditingOptions Sql = new() { Syntax = SourceSyntax.Sql, InsertSpaceAfterLineComment = false };

    private static string NewLine(string marked, CodeEditingOptions? options = null)
    {
        var (text, selection) = Caret.Parse(marked);
        return Caret.Render(text, CodeEditing.InsertNewLine(text, selection, options ?? CSharp));
    }

    private static string? Type(string marked, char typed, CodeEditingOptions? options = null)
    {
        var (text, selection) = Caret.Parse(marked);
        var result = CodeEditing.TypeCharacter(text, selection, typed, options ?? CSharp);
        return result is null ? null : Caret.Render(text, result);
    }

    [Theory]
    [InlineData("    a|", "    a\n    |")]
    [InlineData("{|}", "{\n    |\n}")]
    [InlineData("if (x) {|", "if (x) {\n    |")]
    [InlineData("a|   b", "a\n|b")]
    [InlineData("  |  a", "  \n  |  a")]
    [InlineData("a[bc]d", "a\n|d")]
    public void InsertNewLine_KeepsAndOpensIndentation(string marked, string expected)
        => Assert.Equal(expected, NewLine(marked));

    [Fact]
    public void InsertNewLine_UsesConfiguredSeparator()
        => Assert.Equal("{\r\n    |\r\n}", NewLine("{|}", CSharpCrLf));

    [Fact]
    public void OpenLine_AboveAndBelowKeepIndentation()
    {
        var (text, selection) = Caret.Parse("  a|b\nc");

        Assert.Equal("  |\n  ab\nc", Caret.Render(text, CodeEditing.OpenLine(text, selection, above: true, CSharp)));
        Assert.Equal("  ab\n  |\nc", Caret.Render(text, CodeEditing.OpenLine(text, selection, above: false, CSharp)));
    }

    [Theory]
    [InlineData("|", '(', "(|)")]
    [InlineData("f(|)", '(', "f((|))")]
    [InlineData("(|)", ')', "()|")]
    [InlineData("\"a|\"", '"', "\"a\"|")]
    [InlineData("x = |;", '"', "x = \"|\";")]
    [InlineData("    |", '}', "}|")]
    [InlineData("        |", '}', "    }|")]
    [InlineData("a[bc]d", '(', "a([bc])d")]
    public void TypeCharacter_HandlesPairs(string marked, char typed, string expected)
        => Assert.Equal(expected, Type(marked, typed));

    [Theory]
    [InlineData("|abc", '(')]
    [InlineData("it|", '\'')]
    [InlineData("// |", '(')]
    [InlineData("\"a |\"", '(')]
    [InlineData("a|", 'x')]
    [InlineData("x }|", '}')]
    public void TypeCharacter_LeavesDefaultInsertion(string marked, char typed)
        => Assert.Null(Type(marked, typed));

    [Fact]
    public void TypeCharacter_SqlDoubledQuotesStayInString()
        => Assert.Null(Type("'it''s |'", '(', Sql));

    [Fact]
    public void TypeCharacter_BraceOnNewLineWhenEnabled()
        => Assert.Equal("    if (x)\n    {|}", Type("    if (x) |", '{', CSharp with { PlaceOpenBraceOnNewLine = true }));

    [Theory]
    [InlineData("(|)", "|")]
    [InlineData("\"|\"", "|")]
    [InlineData("        |", "    |")]
    [InlineData("      |", "    |")]
    public void DeleteBackward_RemovesPairsAndIndentLevels(string marked, string expected)
    {
        var (text, selection) = Caret.Parse(marked);
        Assert.Equal(expected, Caret.Render(text, CodeEditing.DeleteBackward(text, selection, CSharp)));
    }

    [Theory]
    [InlineData("a|b")]
    [InlineData("  x|")]
    public void DeleteBackward_LeavesDefaultDeletion(string marked)
    {
        var (text, selection) = Caret.Parse(marked);
        Assert.Null(CodeEditing.DeleteBackward(text, selection, CSharp));
    }

    [Theory]
    [InlineData("ab|", "ab  |")]
    [InlineData("|", "    |")]
    [InlineData("\t|", "\t    |")]
    public void InsertTab_GoesToNextStop(string marked, string expected)
    {
        var (text, selection) = Caret.Parse(marked);
        Assert.Equal(expected, Caret.Render(text, CodeEditing.InsertTab(text, selection, CSharp)));
    }

    [Fact]
    public void IndentLines_IndentsAndOutdentsWholeLines()
    {
        var (text, selection) = Caret.Parse("[a\n\n  b]");

        Assert.Equal("[    a\n\n      b]", Caret.Render(text, CodeEditing.InsertTab(text, selection, CSharp)));
        Assert.Equal("[a\n\nb]", Caret.Render(text, CodeEditing.IndentLines(text, selection, outdent: true, CSharp)));
    }

    [Fact]
    public void ToggleLineComment_UsesMinimalIndentation()
    {
        var (text, selection) = Caret.Parse("  [a\n    b]");
        var commented = CodeEditing.ToggleLineComment(text, selection, comment: true, CSharp);

        Assert.Equal("  // [a\n  //   b]", Caret.Render(text, commented));

        var again = commented!.Apply(text);
        Assert.Equal("  a\n    b", CodeEditing.ToggleLineComment(again, commented.Selection, comment: false, CSharp)!.Apply(again));
    }

    [Fact]
    public void ToggleLineComment_SqlPrefixWithoutSpace()
    {
        var (text, selection) = Caret.Parse("select| 1");
        Assert.Equal("--select| 1", Caret.Render(text, CodeEditing.ToggleLineComment(text, selection, comment: true, Sql)));
    }

    [Fact]
    public void DuplicateSelectionOrLine()
    {
        var (line, caret) = Caret.Parse("a|b\nc");
        Assert.Equal("ab\na|b\nc", Caret.Render(line, CodeEditing.DuplicateSelectionOrLine(line, caret, CSharp)));

        var (text, selection) = Caret.Parse("x[ab]y");
        Assert.Equal("xab[ab]y", Caret.Render(text, CodeEditing.DuplicateSelectionOrLine(text, selection, CSharp)));
    }

    [Theory]
    [InlineData("a\nb|\nc", "a\n|c")]
    [InlineData("a\nb|", "|a")]
    [InlineData("a\r\nb|", "|a")]
    [InlineData("only|", "|")]
    public void DeleteLines(string marked, string expected)
    {
        var (text, selection) = Caret.Parse(marked);
        Assert.Equal(expected, Caret.Render(text, CodeEditing.DeleteLines(text, selection)));
    }

    [Fact]
    public void MoveLines_SwapsWithNeighbourKeepingSeparators()
    {
        var (text, selection) = Caret.Parse("a\r\nb|\r\nc");

        Assert.Equal("b|\r\na\r\nc", Caret.Render(text, CodeEditing.MoveLines(text, selection, up: true)));
        Assert.Equal("a\r\nc\r\nb|", Caret.Render(text, CodeEditing.MoveLines(text, selection, up: false)));
        Assert.Null(CodeEditing.MoveLines("a", TextSelection.Caret(0), up: true));
    }

    [Theory]
    [InlineData("    ab", 6, 4)]
    [InlineData("    ab", 4, 0)]
    [InlineData("x\n  y", 5, 4)]
    public void SmartHome(string text, int caret, int expected)
        => Assert.Equal(expected, CodeEditing.SmartHomePosition(text, caret));
}
