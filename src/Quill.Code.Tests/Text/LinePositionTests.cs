using Quill.Code.Text;

namespace Quill.Code.Tests.Text;

public sealed class LinePositionTests
{
    [Theory]
    [InlineData("abc", 0, 1, 1)]
    [InlineData("abc", 2, 1, 3)]
    [InlineData("abc\ndef", 4, 2, 1)]
    [InlineData("abc\r\ndef", 5, 2, 1)]
    [InlineData("abc\r\ndef", 4, 1, 5)]
    [InlineData("a\rb\nc", 4, 3, 1)]
    [InlineData("abc", 10, 1, 4)]
    [InlineData("abc", -3, 1, 1)]
    public void FromOffset_ReturnsOneBasedLineAndColumn(string text, int offset, int line, int column)
    {
        Assert.Equal(new LinePosition(line, column), LinePosition.FromOffset(text, offset));
    }
}
