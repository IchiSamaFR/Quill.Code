using Quill.Code.Sql.SqlServer;

namespace Quill.Code.Tests.SqlServer;

/// <summary>The | character marks the hovered position.</summary>
public class SqlHoverTests
{
    [Fact]
    public async Task Column_ShowsTypeAndTable()
    {
        var hover = await HoverAsync("SELECT u.Em|ail FROM dbo.Users u");

        Assert.NotNull(hover);
        Assert.Equal("(colonne) Email nvarchar(200) NULL", hover.Signature);
        Assert.Equal("dbo.Users", hover.Description);
    }

    [Fact]
    public async Task Alias_ShowsTableAndColumns()
    {
        var hover = await HoverAsync("SELECT |u.Id FROM dbo.Users u");

        Assert.NotNull(hover);
        Assert.Equal("(table) dbo.Users AS u", hover.Signature);
        Assert.Contains("Id int · clé", hover.Description, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Schema_And_Database()
    {
        Assert.Equal("(schéma) sales", (await HoverAsync("SELECT * FROM sal|es.Orders"))?.Signature);
        Assert.Equal("(base de données) Archive", (await HoverAsync("SELECT * FROM Arch|ive.dbo.OldUsers"))?.Signature);
    }

    [Fact]
    public async Task Variables_DeclaredOrProvided()
    {
        var service = new SqlLanguageService(new SqlLanguageOptions { Variables = [new SqlVariable("@MaxRows", "int", "Paramètre du dashboard")] });

        const string code = "DECLARE @name nvarchar(50) = 'x';\nSELECT @name, @MaxRows";
        var declared = await service.GetHoverAsync(code, code.LastIndexOf("@name", StringComparison.Ordinal) + 2);
        var provided = await service.GetHoverAsync(code, code.IndexOf("@MaxRows", StringComparison.Ordinal) + 2);

        Assert.Equal("(variable) @name nvarchar(50)", declared?.Signature);
        Assert.Equal("(paramètre) @MaxRows int", provided?.Signature);
        Assert.Equal("Paramètre du dashboard", provided?.Description);
    }

    [Fact]
    public async Task WithoutCatalog_NothingOnNames()
        => Assert.Null(await new SqlLanguageService().GetHoverAsync("SELECT Email FROM Users", 9));

    private static Task<Quill.Code.Analysis.HoverInfo?> HoverAsync(string codeWithCaret)
    {
        var position = codeWithCaret.IndexOf('|', StringComparison.Ordinal);
        var service = new SqlLanguageService(new SqlLanguageOptions { Catalog = SqlTestCatalog.Create() });
        return service.GetHoverAsync(codeWithCaret.Remove(position, 1), position);
    }
}
