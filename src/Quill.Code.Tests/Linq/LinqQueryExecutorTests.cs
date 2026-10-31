using Quill.Code.CSharp.Linq;
using Quill.Code.Execution;
using Quill.Code.Scripting;
using Quill.Data.Sql;

namespace Quill.Code.Tests.Linq;

/// <summary>API publique, sans connexion : les requetes n'affichent que des valeurs calculees, rien n'est envoye au serveur.</summary>
public class LinqQueryExecutorTests
{
    private const string ConnectionString = "Server=localhost;Database=RaveOnDb;Trusted_Connection=True;TrustServerCertificate=True";

    private static readonly LinqQueryExecutor Executor = new("Quill.Code.Tests");

    [Fact]
    public async Task Run_DumpsAreRemittedByData()
    {
        var (log, data) = await RunAsync("""
            (40 + 2).Dump("Total");
            new[] { "a", "b" }.Dump();
            """);

        Assert.Equal(2, data.Count);
        Assert.Equal("Total", data[0].Title);
        Assert.Equal("42", Assert.Single(data[0].Rows).Cells[0]);
        Assert.Equal(2, data[1].Rows.Count);
        Assert.DoesNotContain(log, e => e.IsError);
        Assert.Contains(log, e => e.Text.StartsWith("Contexte RaveOnDbContext", StringComparison.Ordinal));
        Assert.Contains(log, e => e.Text == "Total : (1 ligne(s))");
    }

    [Fact]
    public async Task Run_CompilationErrorsAreLoggedNotThrown()
    {
        var (log, data) = await RunAsync("Db.AspNetUsers.Wher(u => true).Dump();");

        Assert.Empty(data);
        var error = Assert.Single(log, e => e.IsError);
        Assert.Equal(LogSource.Code, error.Source);
        Assert.Contains("Wher", error.Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Run_ScriptExceptionIsLogged()
    {
        var (log, _) = await RunAsync("throw new InvalidOperationException(\"boom\");");

        var error = Assert.Single(log, e => e.IsError);
        Assert.Equal("InvalidOperationException : boom", error.Text);
        Assert.Equal(LogSource.Code, error.Source);
    }

    [Fact]
    public async Task Run_ScriptLogIsForwarded()
    {
        var (log, _) = await RunAsync("Log.Information(\"a\");");

        Assert.Contains(log, e => e is { Text: "a", Source: LogSource.Code });
        Assert.Contains(log, e => e.Text == "Aucun résultat : affichez les données avec .Dump().");
    }

    [Fact]
    public async Task Read_ReturnsTheLastDumpWithParameters()
    {
        var result = await Executor.ReadAsync(ConnectionString, "RaveOnDb", TestLinq.Schema, "1.Dump();\n(UserId * 2).Dump(\"Double\");",
            [new QueryParameter("@UserId", QueryParameterType.Integer, 21L)], maxRows: 10);

        Assert.Equal("Double", result.Title);
        Assert.Equal("42", Assert.Single(result.Rows).Cells[0]);
    }

    [Fact]
    public async Task Read_CompilationErrorIsThrown()
        => await Assert.ThrowsAsync<InvalidOperationException>(() => Executor.ReadAsync(ConnectionString, null, TestLinq.Schema, "Db.Nope.Dump();", [], 10));

    [Fact]
    public async Task Read_NothingDumpedIsAnError()
        => await Assert.ThrowsAsync<InvalidOperationException>(() => Executor.ReadAsync(ConnectionString, null, TestLinq.Schema, "var x = 1;", [], 10));

    [Fact]
    public async Task LanguageService_SchemaGivesTheContextAndParameters()
    {
        var service = new LinqLanguageService(new LinqLanguageOptions
        {
            Schema = _ => Task.FromResult<SqlDatabaseSchema?>(TestLinq.Schema),
            Parameters = [new QueryParameter("MaxFailures", QueryParameterType.Integer, 3L)],
        });

        const string query = "var users = Db.";
        Assert.Contains((await service.GetCompletionsAsync(query, query.Length)).Items, i => i.InsertText == "AspNetUsers");
        Assert.DoesNotContain(await service.GetDiagnosticsAsync("Db.AspNetUsers.Where(u => u.AccessFailedCount > MaxFailures).Dump();"), d => d.IsError);
    }

    [Fact]
    public async Task LanguageService_UnreadableSchemaFallsBackToCSharp()
    {
        var service = new LinqLanguageService(new LinqLanguageOptions { Schema = _ => throw new InvalidOperationException("Base injoignable.") });

        const string query = "var now = DateTime.";
        Assert.Contains((await service.GetCompletionsAsync(query, query.Length)).Items, i => i.InsertText == "Now");
    }

    private static async Task<(List<LogEntry> Log, List<QueryResultSet> Data)> RunAsync(string script)
    {
        List<LogEntry> log = [];
        List<QueryResultSet> data = [];
        await Executor.RunAsync(ConnectionString, TestLinq.Schema, script, new DataRunOptions { Log = log.Add, Data = data.Add });
        return (log, data);
    }
}
