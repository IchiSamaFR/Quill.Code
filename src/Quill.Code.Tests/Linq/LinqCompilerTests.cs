using Quill.Code.CSharp.Linq.Compilation;
using Quill.Code.CSharp.Linq.Execution;
using Quill.Code.Analysis;
using Quill.Code.CSharp.Compilation;
using Quill.Code.Execution;
using Quill.Code.Scripting;
using Microsoft.EntityFrameworkCore;

namespace Quill.Code.Tests.Linq;

/// <summary>Requetes compilees contre le modele genere, traduites en SQL sans connexion.</summary>
public class LinqCompilerTests
{
    private const string ConnectionString = "Server=localhost;Database=RaveOnDb;Trusted_Connection=True;TrustServerCertificate=True";

    private static Lazy<LinqGeneratedModelAssembly> Model => TestLinq.Model;

    [Fact]
    public async Task MethodSyntax_TranslatesToSql()
    {
        var sql = await TranslateAsync("""
            Db.AspNetUsers
                .Where(u => u.Email != null && !u.EmailConfirmed)
                .OrderBy(u => u.Email)
                .Select(u => new { u.Id, u.Email })
                .Dump();
            """);

        Assert.Contains("WHERE", sql, StringComparison.Ordinal);
        Assert.Contains("ORDER BY", sql, StringComparison.Ordinal);
        Assert.Contains("[Email]", sql, StringComparison.Ordinal);
    }

    [Fact]
    public async Task QuerySyntax_TranslatesToSql()
    {
        var sql = await TranslateAsync("(from u in Db.AspNetUsers where u.AccessFailedCount > 3 select u.Id).Dump();");

        Assert.Contains("[AccessFailedCount] > 3", sql, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Join_OnCompositeKeyTable()
    {
        var sql = await TranslateAsync("""
            (from u in Db.AspNetUsers
             join r in Db.AspNetUserRoles on u.Id equals r.UserId
             select new { u.Email, r.RoleId }).Dump();
            """);

        Assert.Contains("INNER JOIN", sql, StringComparison.Ordinal);
        Assert.Contains("[AspNetUserRoles]", sql, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Scalar_IsDumped()
    {
        var script = "(40 + 2).Dump();";

        Assert.Equal([42], await DumpsAsync(script));
    }

    [Fact]
    public async Task Statements_ComposeBeforeDump()
    {
        var sql = await TranslateAsync("""
            var counts = Db.AspNetUsers.Select(u => u.AccessFailedCount);
            counts.Where(i => i > 10).Dump();
            """);

        Assert.Contains("> 10", sql, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Usings_DeclaredByTheQuery()
    {
        var script = """
            using System.Text;

            new StringBuilder("a").Append('b').ToString().Dump();
            """;

        Assert.Equal(["ab"], await DumpsAsync(script));
    }

    [Fact]
    public async Task Types_DeclaredAfterTheStatements()
    {
        var script = """
            new Greeter().Say().Dump();

            class Greeter
            {
                public string Say() => "hi";
            }
            """;

        Assert.Equal(["hi"], await DumpsAsync(script));
    }

    [Fact]
    public async Task Context_IsDbOfTheGeneratedType()
    {
        var script = "Db.GetType().Name.Dump();";

        Assert.Equal(["RaveOnDbContext"], await DumpsAsync(script));
    }

    [Fact]
    public async Task ReturnedValue_IsIgnored()
    {
        var script = "return 42;";

        Assert.Empty(await DumpsAsync(script));
    }

    [Fact]
    public async Task Async_IsAwaited()
    {
        var script = """
            await Task.Yield();
            42.Dump();
            """;

        Assert.Equal([42], await DumpsAsync(script));
    }

    [Theory]
    [InlineData("")]
    [InlineData("using System;")]
    [InlineData("class Greeter { }")]
    public async Task WithoutStatements_IsAnError(string query)
    {
        var compiled = await LinqCompiler.CompileQueryAsync(TestLinq.Globals(Model.Value), query);

        Assert.False(compiled.Success);
        Assert.Equal("CS5001", Assert.Single(compiled.Errors).Code);
    }

    [Fact]
    public async Task Parameters_ExposedByNameAndType()
    {
        // Nom sans @, type du dashboard (int?) : l'arithmetique reste celle du type.
        LinqParameter[] parameters = [new LinqParameter("@UserId", typeof(int?), 16)];
        var globals = TestLinq.Globals(Model.Value, parameters);
        Assert.Equal([32], await DumpsAsync("(UserId * 2).Dump();", globals, parameters));

        // Meme QueryGlobals, autre valeur : posee a l'execution.
        Assert.Equal([8], await DumpsAsync("(UserId * 2).Dump();", globals, [new LinqParameter("@UserId", typeof(int?), 4)]));
    }

    [Fact]
    public void Parameters_SameNamesAndTypesShareTheirAssembly()
    {
        // Les valeurs ne font pas partie du source : un seul QueryGlobals par jeu de noms et de types.
        Assert.Equal(
            LinqQuerySource.BuildGlobals(Model.Value.Model, [new LinqParameter("UserId", typeof(int?), 16)]),
            LinqQuerySource.BuildGlobals(Model.Value.Model, [new LinqParameter("UserId", typeof(int?), 4)]));
    }

    [Fact]
    public async Task Parameters_SentAsSqlParameters()
    {
        LinqParameter[] parameters = [new LinqParameter("Email", typeof(string), "a@b.fr")];
        var globals = TestLinq.Globals(Model.Value, parameters);
        var sql = Assert.IsAssignableFrom<IQueryable>(Assert.Single(await DumpsAsync("Db.AspNetUsers.Where(u => u.Email == Email).Dump();", globals, parameters))).ToQueryString();

        // ToQueryString declare la valeur en tete, mais le WHERE compare bien a un parametre.
        Assert.Contains("= @", sql, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Parameters_UnconvertibleValueIsAnError()
    {
        LinqParameter[] parameters = [new LinqParameter("@Count", typeof(int?), "abc")];
        var globals = TestLinq.Globals(Model.Value, parameters);
        var ex = await Assert.ThrowsAsync<LinqCompilationException>(() => DumpsAsync("Count.Dump();", globals, parameters));

        Assert.Contains("Count", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Errors_PointToQueryText()
    {
        const string query = "var users = Db.AspNetUsers;\nusers.Wher(u => true);";
        var compiled = await LinqCompiler.CompileQueryAsync(TestLinq.Globals(Model.Value), query);

        Assert.False(compiled.Success);
        var error = compiled.Errors[0];
        Assert.Equal("CS1061", error.Code);
        Assert.Equal("Wher", query.Substring(error.Start, error.Length));
    }

    [Fact]
    public async Task Run_CompilationErrorsAreThrown()
    {
        var ex = await Assert.ThrowsAsync<CSharpCompilationException>(() => DumpsAsync("Db.AspNetUsers.Wher(u => true).Dump();"));

        Assert.Equal("CS1061", Assert.Single(ex.Errors).Code);
    }

    [Fact]
    public async Task Errors_UnknownContext()
    {
        const string query = "Northwind.Orders.Dump();";
        var compiled = await LinqCompiler.CompileQueryAsync(TestLinq.Globals(Model.Value), query);

        Assert.Contains(compiled.Errors, e => e.Code == "CS0103" && e.Start == query.IndexOf("Northwind", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Dump_CollectsEachValueAndContinues()
    {
        List<(object? Value, string? Title)> dumps = [];
        using var context = LinqCompiler.CreateContext(Model.Value, ConnectionString);
        await LinqCompiler.RunAsync(TestLinq.Globals(Model.Value), """
            Db.AspNetUsers.Dump("Utilisateurs");
            var total = 40.Dump();
            (total + 2).Dump();
            """, context, (value, title) => dumps.Add((value, title)));

        Assert.Equal(3, dumps.Count);
        Assert.IsAssignableFrom<IQueryable>(dumps[0].Value);
        Assert.Equal("Utilisateurs", dumps[0].Title);
        Assert.Equal(40, dumps[1].Value);
        Assert.Null(dumps[1].Title);
        Assert.Equal(42, dumps[2].Value);
    }

    [Fact]
    public async Task Log_IsRedirectedWithItsLevel()
    {
        List<LogEntry> output = [];
        await DumpsAsync("""
            Log.Information("a");
            Log.Warning($"{1}+{2}");
            Log.Write(LogLevel.Error, "{b}");
            Log.Write(LogLevel.None, "ignore");
            """, log: output.Add);

        Assert.Equal(new[] { (LogLevel.Information, "a"), (LogLevel.Warning, "1+2"), (LogLevel.Error, "{b}") }, output.Select(e => (e.Level, e.Text)));
        Assert.All(output, e => Assert.Equal(LogSource.Code, e.Source));
    }

    private static async Task<string> TranslateAsync(string query)
    {
        return Assert.IsAssignableFrom<IQueryable>(Assert.Single(await DumpsAsync(query))).ToQueryString();
    }

    /// <summary>
    /// Execute sans ouvrir de connexion et rend les valeurs affichees : une requete IQueryable n'est que traduite
    /// (ToQueryString), apres coup, d'ou un contexte non libere ici.
    /// </summary>
    private static async Task<List<object?>> DumpsAsync(string query, LinqParametersAssembly? globals = null, IReadOnlyList<LinqParameter>? parameters = null, Action<LogEntry>? log = null)
    {
        List<object?> values = [];
        await LinqCompiler.RunAsync(globals ?? TestLinq.Globals(Model.Value), query, LinqCompiler.CreateContext(Model.Value, ConnectionString), (value, _) => values.Add(value), parameters, log is null ? null : new RunOptions { Log = log });
        return values;
    }
}
