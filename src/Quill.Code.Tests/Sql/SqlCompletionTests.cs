using Quill.Code.Completion;
using Quill.Code.Sql;
using Quill.Code.Sql.Schema;
using Quill.Data.Sql;

namespace Quill.Code.Tests.Sql;

/// <summary>Le caractère | marque le curseur.</summary>
public class SqlCompletionTests
{
    [Fact]
    public async Task TableSource_ListsTablesViewsSchemasAndDatabases()
    {
        var items = await CompleteAsync("SELECT * FROM |");

        Assert.Contains(items, i => i.Label == "Users" && i.Kind == CompletionItemKind.Table && i.TextToInsert == "Users");
        Assert.Contains(items, i => i.Label == "Orders" && i.TextToInsert == "sales.Orders");
        Assert.Contains(items, i => i.Label == "ActiveUsers" && i.Kind == CompletionItemKind.View);
        Assert.Contains(items, i => i.Label == "sales" && i.Kind == CompletionItemKind.Schema);
        Assert.Contains(items, i => i.Label == "Archive" && i.Kind == CompletionItemKind.Database);
        Assert.DoesNotContain(items, i => i.Label == "PurgeUsers");
    }

    [Fact]
    public async Task Alias_ListsColumnsQuotedWhenReserved()
    {
        var items = await CompleteAsync("SELECT u.| FROM dbo.Users u");

        Assert.Contains(items, i => i.Label == "Email" && i.Kind == CompletionItemKind.Column && i.Detail == "nvarchar(200)");
        Assert.Contains(items, i => i.Label == "Order" && i.TextToInsert == "[Order]");
    }

    [Fact]
    public async Task Column_ListsColumnsOfTheBatchTables()
    {
        var items = await CompleteAsync("SELECT * FROM Users u JOIN sales.Orders o ON o.UserId = u.Id WHERE |");

        Assert.Contains(items, i => i.Label == "Email" && i.Detail == "nvarchar(200) · u");
        Assert.Contains(items, i => i.Label == "UserId" && i.Detail == "int · o");
        Assert.Contains(items, i => i.Label == "AND" && i.Kind == CompletionItemKind.Keyword);
    }

    [Fact]
    public async Task OtherDatabase_ListsItsObjects()
    {
        var items = await CompleteAsync("SELECT * FROM Archive.dbo.|");

        Assert.Contains(items, i => i.Label == "OldUsers");
        Assert.DoesNotContain(items, i => i.Label == "Users");
    }

    [Fact]
    public async Task Use_ChangesTheDatabase()
    {
        var items = await CompleteAsync("USE Archive\nSELECT * FROM |");

        Assert.Contains(items, i => i.Label == "OldUsers");
    }

    [Fact]
    public async Task Exec_ListsProcedures()
    {
        var items = await CompleteAsync("EXEC |");

        Assert.Contains(items, i => i.Label == "PurgeUsers" && i.Kind == CompletionItemKind.Procedure);
        Assert.DoesNotContain(items, i => i.Label == "Users");
    }

    [Fact]
    public async Task DeclaredVariables_AreListedAfterAt()
    {
        var service = new SqlLanguageService(new SqlLanguageOptions { Variables = [new SqlVariable("MaxRows", "int")] });

        const string code = "DECLARE @Limite int = 10;\nSELECT @@VERSION;\nSELECT @";
        var items = (await service.GetCompletionsAsync(code, code.Length)).Items;

        Assert.Contains(items, i => i.Label == "@Limite" && i.Kind == CompletionItemKind.Variable && i.Detail == "int");
        Assert.Contains(items, i => i.Label == "@MaxRows" && i.Detail == "int");
        Assert.DoesNotContain(items, i => i.Label == "@@VERSION");
    }

    [Fact]
    public async Task WithoutCatalog_KeywordsAndDocumentWords()
    {
        var service = new SqlLanguageService();

        const string code = "SELECT Email FROM Users\nSELECT ";
        var list = await service.GetCompletionsAsync(code, code.Length);

        Assert.Contains(list.Items, i => i.Label == "COUNT" && i.Kind == CompletionItemKind.Function);
        Assert.Contains(list.Items, i => i.Label == "Email" && i.Kind == CompletionItemKind.Text);
        Assert.Equal(code.Length, list.SpanStart);
    }

    [Fact]
    public async Task FailingCatalog_FallsBackToKeywords()
    {
        var service = new SqlLanguageService(new SqlLanguageOptions { Catalog = new FailingCatalog() });

        var items = (await service.GetCompletionsAsync("SELECT * FROM ", 14)).Items;

        Assert.DoesNotContain(items, i => i.Kind == CompletionItemKind.Table);
    }

    private static async Task<IReadOnlyList<CompletionItem>> CompleteAsync(string codeWithCaret)
    {
        var caret = codeWithCaret.IndexOf('|', StringComparison.Ordinal);
        var service = new SqlLanguageService(new SqlLanguageOptions { Catalog = SqlTestCatalog.Create() });
        return (await service.GetCompletionsAsync(codeWithCaret.Remove(caret, 1), caret)).Items;
    }

    private sealed class FailingCatalog : ISqlCatalog
    {
        public Task<IReadOnlyList<SqlDatabaseInfo>> GetDatabasesAsync(CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("Serveur injoignable.");

        public Task<SqlDatabaseSchema?> GetDatabaseAsync(string? database, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("Serveur injoignable.");
    }
}
