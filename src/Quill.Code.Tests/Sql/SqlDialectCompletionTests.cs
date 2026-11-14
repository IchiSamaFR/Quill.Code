using Quill.Code.Completion;
using Quill.Code.Sql.Dialects;
using Quill.Code.Sql.MySql;
using Quill.Code.Sql.Oracle;
using Quill.Code.Sql.PostgreSql;
using Quill.Code.Sql.Sqlite;
using Quill.Code.Sql.SqlServer;
using Quill.Code.Sql.SqlServer.Completion;
using Quill.Code.Sql.SqlServer.Lexing;
using Quill.Code.Sql.SqlServer.Schema;
using Quill.Data.Sql;

namespace Quill.Code.Tests.Sql;

/// <summary>The | character marks the caret.</summary>
public class SqlDialectCompletionTests
{
    [Fact]
    public async Task Oracle_ProposesFetchFirstAfterATableName()
    {
        var items = await CompleteAsync(OracleDialect.Instance, "SELECT * FROM EMPLOYEES F|");

        Assert.Contains(items, i => i.Label == "FETCH" && i.Kind == CompletionItemKind.Keyword);
        Assert.Contains(items, i => i.Label == "FETCH FIRST 10 ROWS ONLY");
        Assert.DoesNotContain(items, i => i.Label == "TOP");
    }

    [Fact]
    public async Task SqlServer_KeepsTopAndIgnoresLimit()
    {
        var items = await CompleteAsync(SqlServerDialect.Instance, "SELECT |");

        Assert.Contains(items, i => i.Label == "TOP");
        Assert.DoesNotContain(items, i => i.Label == "LIMIT");
    }

    [Fact]
    public async Task MySql_ProposesLimitAndQuotesWithBackticks()
    {
        var items = await CompleteAsync(MySqlDialect.Instance, "SELECT * FROM users L|");
        Assert.Contains(items, i => i.Label == "LIMIT");

        items = await CompleteAsync(MySqlDialect.Instance, "SELECT u.| FROM users u");
        Assert.Contains(items, i => i.Label == "order" && i.TextToInsert == "`order`");
    }

    [Fact]
    public async Task MySql_DoesNotQualifyWithTheCurrentDatabase()
    {
        var items = await CompleteAsync(MySqlDialect.Instance, "SELECT * FROM |");

        Assert.Contains(items, i => i.Label == "users" && i.TextToInsert == "users");
        Assert.DoesNotContain(items, i => i.Kind == CompletionItemKind.Schema);
    }

    [Fact]
    public async Task MySql_ResolvesRemoteDatabaseTableColumns()
    {
        var service = new SqlLanguageService(new SqlLanguageOptions
        {
            Dialect = MySqlDialect.Instance,
            Catalog = CreateMultiDatabaseCatalog(),
        });
        const string code = "SELECT archive.orders.|";

        var list = await service.GetCompletionsAsync(code, code.Length - 1);
        var item = Assert.Single(list.Items, i => i.Label == "Id");
        Assert.Equal(CompletionItemKind.Column, item.Kind);
    }

    [Fact]
    public async Task MySql_HoversRemoteDatabaseTablesAndColumns()
    {
        var service = new SqlLanguageService(new SqlLanguageOptions
        {
            Dialect = MySqlDialect.Instance,
            Catalog = CreateMultiDatabaseCatalog(),
        });
        const string tableQuery = "SELECT * FROM archive.ord|ers";
        const string columnQuery = "SELECT archive.orders.I|d";
        var tableHover = await service.GetHoverAsync(tableQuery.Remove(tableQuery.IndexOf('|'), 1), tableQuery.IndexOf('|'));
        var columnHover = await service.GetHoverAsync(columnQuery.Remove(columnQuery.IndexOf('|'), 1), columnQuery.IndexOf('|'));

        Assert.Equal("(table) archive.orders", tableHover?.Signature);
        Assert.Equal("(column) Id int NULL", columnHover?.Signature);
    }

    [Fact]
    public async Task PostgreSql_QuotesMixedCaseNames()
    {
        var items = await CompleteAsync(PostgreSqlDialect.Instance, "SELECT u.| FROM users u");

        Assert.Contains(items, i => i.Label == "id" && i.TextToInsert == "id");
        Assert.Contains(items, i => i.Label == "CreatedAt" && i.TextToInsert == "\"CreatedAt\"");
    }

    [Fact]
    public async Task Oracle_ListsParametersAfterAColon()
    {
        var service = new SqlLanguageService(new SqlLanguageOptions
        {
            Dialect = OracleDialect.Instance,
            Variables = [new SqlVariable("@UserId", "NUMBER")],
        });

        const string code = "SELECT * FROM EMPLOYEES WHERE ID = :Us";
        var list = await service.GetCompletionsAsync(code, code.Length);

        var item = Assert.Single(list.Items);
        Assert.Equal(":UserId", item.Label);
        Assert.Equal("UserId", item.TextToInsert);
        Assert.Equal("UserId", item.TextToFilter);
        Assert.Equal(code.Length - 2, list.SpanStart);
    }

    [Fact]
    public async Task Oracle_HoverDescribesAColonParameter()
    {
        var service = new SqlLanguageService(new SqlLanguageOptions
        {
            Dialect = OracleDialect.Instance,
            Variables = [new SqlVariable("UserId", "NUMBER", "Parameter")],
        });

        const string code = "SELECT * FROM EMPLOYEES WHERE ID = :UserId";
        var hover = await service.GetHoverAsync(code, code.Length - 2);

        Assert.NotNull(hover);
        Assert.Equal("(parameter) :UserId NUMBER", hover.Signature);
    }

    [Fact]
    public void Lexer_FollowsTheDialect()
    {
        const string mySql = "SELECT `a b`, \"x\" # fin";
        var tokens = SqlLexer.Tokenize(mySql, MySqlDialect.Instance);
        Assert.Contains(tokens, t => t.Kind == SqlTokenKind.QuotedIdentifier && t.GetText(mySql) == "`a b`");
        Assert.Contains(tokens, t => t.Kind == SqlTokenKind.String && t.GetText(mySql) == "\"x\"");
        Assert.Contains(tokens, t => t.Kind == SqlTokenKind.LineComment && t.GetText(mySql) == "# fin");

        const string postgreSql = "SELECT $$a 'b'$$, x::int";
        tokens = SqlLexer.Tokenize(postgreSql, PostgreSqlDialect.Instance);
        Assert.Contains(tokens, t => t.Kind == SqlTokenKind.String && t.GetText(postgreSql) == "$$a 'b'$$");
        Assert.DoesNotContain(tokens, t => t.Kind == SqlTokenKind.Variable);

        const string oracle = "x := :p";
        tokens = SqlLexer.Tokenize(oracle, OracleDialect.Instance);
        Assert.Single(tokens, t => t.Kind == SqlTokenKind.Variable && t.GetText(oracle) == ":p");
    }

    [Fact]
    public void MySqlLexer_RecognizesDashCommentsAndBackslashEscapes()
    {
        const string code = "SELECT 5--2 + value FROM t, 'it\\'s text', name";
        var tokens = SqlLexer.Tokenize(code, MySqlDialect.Instance);

        Assert.DoesNotContain(tokens, t => t.Kind == SqlTokenKind.LineComment);
        Assert.Contains(tokens, t => t.Kind == SqlTokenKind.String && t.GetText(code) == "'it\\'s text'");
        Assert.Contains(tokens, t => t.Kind == SqlTokenKind.Word && t.GetText(code) == "name");
    }

    [Fact]
    public void MySqlLexer_CanDisableBackslashEscapes()
    {
        var dialect = new SqlDialect
        {
            Name = "MySQL without backslash escapes",
            IdentifierQuotes = MySqlDialect.Instance.IdentifierQuotes,
            DoubleQuotedStrings = true,
            DoubleDashRequiresWhitespace = true,
        };
        const string code = "'a\\' + b";
        var tokens = SqlLexer.Tokenize(code, dialect);

        Assert.Contains(tokens, t => t.Kind == SqlTokenKind.String && t.IsTerminated);
        Assert.Contains(tokens, t => t.Kind == SqlTokenKind.Word && t.GetText(code) == "b");
    }

    [Fact]
    public void Sqlite_QuotesEscapeKeyword()
    {
        Assert.Equal("\"Escape\"", SqliteDialect.Instance.QuoteIdentifier("Escape"));
    }

    [Fact]
    public void Analyzer_DoesNotTakeLimitForAnAlias()
    {
        const string code = "SELECT  FROM users LIMIT 10";
        var context = SqlContextAnalyzer.Analyze(code, 7, dialect: MySqlDialect.Instance);

        var table = Assert.Single(context.Tables);
        Assert.Equal("users", table.Name);
        Assert.Null(table.Alias);
    }

    private static async Task<IReadOnlyList<CompletionItem>> CompleteAsync(SqlDialect dialect, string codeWithCaret)
    {
        var caret = codeWithCaret.IndexOf('|', StringComparison.Ordinal);
        var service = new SqlLanguageService(new SqlLanguageOptions { Dialect = dialect, Catalog = CreateCatalog() });
        return (await service.GetCompletionsAsync(codeWithCaret.Remove(caret, 1), caret)).Items;
    }

    /// <summary>"shop" database whose schema has the same name, as a MySQL reader returns it.</summary>
    private static SqlCatalog CreateCatalog() => new(
    [
        new SqlDatabaseSchema("shop",
        [
            new SqlSchemaObject("shop", "users", SqlSchemaObjectKind.Table,
            [
                new SqlColumn("id", "int") { IsNullable = false, PrimaryKeyOrdinal = 1 },
                new SqlColumn("order", "int"),
                new SqlColumn("CreatedAt", "timestamp"),
            ]),
            new SqlSchemaObject("shop", "EMPLOYEES", SqlSchemaObjectKind.Table, [new SqlColumn("ID", "NUMBER")]),
        ]),
    ]);

    private static SqlCatalog CreateMultiDatabaseCatalog() => new(
    [
        new SqlDatabaseSchema("shop", []),
        new SqlDatabaseSchema("archive",
        [
            new SqlSchemaObject("archive", "orders", SqlSchemaObjectKind.Table, [new SqlColumn("Id", "int")]),
        ]),
    ]);
}
