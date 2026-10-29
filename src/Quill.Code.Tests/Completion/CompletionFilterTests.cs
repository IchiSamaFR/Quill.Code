using Quill.Code.Completion;

namespace Quill.Code.Tests.Completion;

public sealed class CompletionFilterTests
{
    private static CompletionItem Item(string label, CompletionItemKind kind = CompletionItemKind.Keyword) => new() { Label = label, Kind = kind };

    [Fact]
    public void Filter_PrefixThenSegmentThenContains()
    {
        var items = new[] { Item("SUM"), Item("ROW_NUMBER"), Item("NUMERIC"), Item("SELECT") };

        var filtered = CompletionFilter.Filter(items, "num");

        Assert.Equal(["NUMERIC", "ROW_NUMBER"], filtered.Select(i => i.Label));
    }

    [Fact]
    public void Filter_SameScoreSortedByKindThenLabel()
    {
        var items = new[] { Item("Users", CompletionItemKind.Table), Item("UPDATE"), Item("UNION") };

        var filtered = CompletionFilter.Filter(items, "u");

        Assert.Equal(["Users", "UNION", "UPDATE"], filtered.Select(i => i.Label));
    }

    [Fact]
    public void Filter_UsesFilterText()
    {
        var items = new[] { new CompletionItem { Label = "List<>", FilterText = "List", Kind = CompletionItemKind.Class } };

        Assert.Single(CompletionFilter.Filter(items, "List"));
        Assert.Empty(CompletionFilter.Filter(items, "<>"));
    }

    [Fact]
    public void Filter_KeepsAtMostMaxResults()
    {
        var items = Enumerable.Range(0, 10).Select(i => Item($"A{i}")).ToArray();

        Assert.Equal(3, CompletionFilter.Filter(items, "a", maxResults: 3).Count);
    }
}
