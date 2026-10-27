using Quill.Code.Editing;

namespace Quill.Code.Tests.Editing;

public sealed class EditResultTests
{
    [Fact]
    public void ToSingleEdit_CoversAllEdits()
    {
        var result = new EditResult([new TextEdit(0, 0, "//"), new TextEdit(3, 0, "//")], TextSelection.Caret(0));

        var edit = result.ToSingleEdit("ab\ncd");

        Assert.Equal(new TextEdit(0, 3, "//ab\n//"), edit);
        Assert.Equal("//ab\n//cd", result.Apply("ab\ncd"));
    }

    [Fact]
    public void ToSingleEdit_NothingForCaretMove()
        => Assert.Null(new EditResult([], TextSelection.Caret(3)).ToSingleEdit("abc"));

    [Theory]
    [InlineData("abc", "abc", null, 0, "")]
    [InlineData("abc", "abXc", 2, 0, "X")]
    [InlineData("a(b", "a()b", 2, 0, ")")]
    [InlineData("abcd", "ad", 1, 2, "")]
    public void Between_FindsSmallestEdit(string oldText, string newText, int? start, int length, string inserted)
    {
        var edit = TextEdit.Between(oldText, newText);

        if (start is null)
        {
            Assert.Null(edit);
            return;
        }

        Assert.Equal(new TextEdit(start.Value, length, inserted), edit);
    }
}
