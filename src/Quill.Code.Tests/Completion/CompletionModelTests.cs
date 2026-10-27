using Quill.Code.Completion;

namespace Quill.Code.Tests.Completion;

public sealed class CompletionModelTests
{
    [Fact]
    public void CompletionItem_FallsBackToLabel()
    {
        var item = new CompletionItem { Label = "Where<>", Kind = CompletionItemKind.ExtensionMethod };

        Assert.Equal("Where<>", item.TextToInsert);
        Assert.Equal("Where<>", item.TextToFilter);
    }

    [Fact]
    public void CompletionItem_UsesExplicitTexts()
    {
        var item = new CompletionItem { Label = "Where<>", Kind = CompletionItemKind.ExtensionMethod, InsertText = "Where", FilterText = "Where" };

        Assert.Equal("Where", item.TextToInsert);
        Assert.Equal("Where", item.TextToFilter);
    }

    [Fact]
    public void CompletionChange_CaretDefaultsToEndOfInsertion()
    {
        Assert.Equal(13, new CompletionChange(10, 2, "Foo").CaretPosition);
        Assert.Equal(11, new CompletionChange(10, 2, "Foo()", 11).CaretPosition);
    }

    [Theory]
    [InlineData(CompletionItemKind.Class, true, false)]
    [InlineData(CompletionItemKind.TypeParameter, true, false)]
    [InlineData(CompletionItemKind.ExtensionMethod, false, true)]
    [InlineData(CompletionItemKind.Procedure, false, true)]
    [InlineData(CompletionItemKind.Column, false, false)]
    public void KindGroups(CompletionItemKind kind, bool isType, bool isCallable)
    {
        Assert.Equal(isType, kind.IsType());
        Assert.Equal(isCallable, kind.IsCallable());
    }
}
