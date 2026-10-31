using Quill.Code.CSharp.Linq.Compilation;
using Quill.Code.CSharp.Linq.Execution;
using Quill.Code.Completion;

namespace Quill.Code.Tests.Linq;

/// <summary>Le caractere | marque le curseur ; Roslyn complete dans le contexte genere.</summary>
public class LinqCompletionTests
{
    private static Lazy<LinqGeneratedModelAssembly> Model => TestLinq.Model;

    [Fact]
    public async Task Context_ListsTables()
    {
        var items = await CompleteAsync(TestLinq.Expression("Db.|"));

        Assert.Contains(items, i => i.InsertText == "AspNetUsers" && i.Kind == CompletionItemKind.Property);
        Assert.Contains(items, i => i.InsertText == "Sales_Orders");
    }

    [Fact]
    public async Task Parameters_AreListed()
    {
        var items = await CompleteAsync("var users = Db.AspNetUsers.Where(u => u.AccessFailedCount > max|);", [new LinqParameter("@MaxFailures", typeof(int?), 3)]);

        Assert.Contains(items, i => i.InsertText == "MaxFailures");
    }

    [Fact]
    public async Task Usings_DeclaredByTheQuery()
    {
        var items = await CompleteAsync("""
            using System.Text;

            var builder = new StringBu|
            """);

        Assert.Contains(items, i => i.InsertText == "StringBuilder");
    }

    [Fact]
    public async Task Lambda_ListsColumns()
    {
        var items = await CompleteAsync(TestLinq.Expression("Db.AspNetUsers.Where(u => u.|)"));

        Assert.Contains(items, i => i.InsertText == "Email" && i.Kind == CompletionItemKind.Property);
        Assert.Contains(items, i => i.InsertText == "EmailConfirmed");
    }

    [Fact]
    public async Task Queryable_ListsLinqOperators()
    {
        var items = await CompleteAsync(TestLinq.Expression("Db.AspNetUsers.Wh|"));

        Assert.Contains(items, i => i.InsertText == "Where" && i.Kind is CompletionItemKind.Method or CompletionItemKind.ExtensionMethod);
    }

    [Fact]
    public async Task QuerySyntax_RangeVariableMembers()
    {
        var items = await CompleteAsync(TestLinq.Expression("from u in Db.AspNetUsers where u.|"));

        Assert.Contains(items, i => i.InsertText == "AccessFailedCount");
    }

    [Fact]
    public async Task Statements_LocalVariableMembers()
    {
        var items = await CompleteAsync(TestLinq.Statements("var ids = Db.AspNetUsers.Select(u => u.Id);\nids.Wh|"));

        Assert.Contains(items, i => i.InsertText == "Where");
    }

    [Fact]
    public async Task Statements_DeclarationInProgress()
    {
        var items = await CompleteAsync(TestLinq.Statements("var users = Db.|"));

        Assert.Contains(items, i => i.InsertText == "AspNetUsers");
    }

    [Fact]
    public async Task WithoutModel_StillCompletesCSharp()
    {
        var query = TestLinq.Expression("DateTime.");
        var items = await TestLinq.CompleteAsync(null, query, query.IndexOf("DateTime.", StringComparison.Ordinal) + "DateTime.".Length);

        Assert.Contains(items, i => i.InsertText == "Now");
    }

    private static async Task<IReadOnlyList<CompletionItem>> CompleteAsync(string queryWithCaret, IReadOnlyList<LinqParameter>? parameters = null)
    {
        var caret = queryWithCaret.IndexOf('|', StringComparison.Ordinal);
        return await TestLinq.CompleteAsync(Model.Value, queryWithCaret.Remove(caret, 1), caret, parameters);
    }
}
