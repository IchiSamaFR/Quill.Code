using Quill.Code.Sql;

namespace Quill.Code.Tests.Sql;

/// <summary>Le caractere | marque la position du curseur dans chaque requete.</summary>
public class SqlContextAnalyzerTests
{
    [Theory]
    [InlineData("SELECT |", SqlContextKind.Column)]
    [InlineData("SELECT na|", SqlContextKind.Column)]
    [InlineData("SELECT COUNT(|", SqlContextKind.Column)]
    [InlineData("SELECT a, |", SqlContextKind.Column)]
    [InlineData("SELECT a FROM t WHERE |", SqlContextKind.Column)]
    [InlineData("SELECT a FROM t WHERE x = 1 AND |", SqlContextKind.Column)]
    [InlineData("SELECT a FROM t ORDER BY |", SqlContextKind.Column)]
    [InlineData("SELECT * FROM a JOIN b ON |", SqlContextKind.Column)]
    [InlineData("UPDATE t SET |", SqlContextKind.Column)]
    [InlineData("INSERT INTO t (|", SqlContextKind.Column)]
    [InlineData("INSERT INTO t (a, |", SqlContextKind.Column)]
    [InlineData("SELECT * FROM t WHERE x = @a |", SqlContextKind.Column)]
    [InlineData("/* note */ SELECT |", SqlContextKind.Column)]
    [InlineData("SELECT * FROM |", SqlContextKind.TableSource)]
    [InlineData("SELECT * FROM a JOIN |", SqlContextKind.TableSource)]
    [InlineData("SELECT * FROM a, |", SqlContextKind.TableSource)]
    [InlineData("INSERT INTO |", SqlContextKind.TableSource)]
    [InlineData("UPDATE |", SqlContextKind.TableSource)]
    [InlineData("USE |", SqlContextKind.Database)]
    [InlineData("EXEC |", SqlContextKind.Procedure)]
    [InlineData("DECLARE @x |", SqlContextKind.DataType)]
    [InlineData("DECLARE @x INT, @y |", SqlContextKind.DataType)]
    [InlineData("CREATE PROCEDURE p @a |", SqlContextKind.DataType)]
    [InlineData("SELECT CAST(a AS |", SqlContextKind.DataType)]
    [InlineData("SELECT CONVERT(|", SqlContextKind.DataType)]
    [InlineData("SELECT * FROM Users |", SqlContextKind.AfterTableName)]
    [InlineData("SELECT * FROM Users W|", SqlContextKind.AfterTableName)]
    [InlineData("SELECT * FROM dbo.Users |", SqlContextKind.AfterTableName)]
    [InlineData("SELECT * FROM a JOIN [b] |", SqlContextKind.AfterTableName)]
    [InlineData("|", SqlContextKind.General)]
    [InlineData("SEL|", SqlContextKind.General)]
    [InlineData("SELECT * FROM Users u |", SqlContextKind.General)]
    [InlineData("SELECT * FROM Users\n|", SqlContextKind.General)]
    [InlineData("SELECT * FROM t;\n|", SqlContextKind.General)]
    [InlineData("UPDATE t |", SqlContextKind.General)]
    [InlineData("CREATE VIEW v AS |", SqlContextKind.General)]
    [InlineData("SELECT 1\nGO\n|", SqlContextKind.General)]
    [InlineData("-- SELECT |", SqlContextKind.None)]
    [InlineData("/* SELECT |", SqlContextKind.None)]
    [InlineData("SELECT 'abc|", SqlContextKind.None)]
    [InlineData("SELECT N'it''s |", SqlContextKind.None)]
    [InlineData("SELECT [ab|", SqlContextKind.None)]
    [InlineData("SELECT @v|", SqlContextKind.None)]
    [InlineData("SELECT a AS |", SqlContextKind.None)]
    [InlineData("SELECT * FROM Users AS |", SqlContextKind.None)]
    public void Kind(string query, SqlContextKind expected)
        => Assert.Equal(expected, Analyze(query).Kind);

    [Fact]
    public void Qualifier_AliasAndColumn()
    {
        var context = Analyze("SELECT * FROM dbo.Users u WHERE u.|");

        Assert.Equal(SqlContextKind.MemberAccess, context.Kind);
        Assert.Equal(new[] { "u" }, context.Qualifier);
        Assert.Equal("Users", context.FindTable("u")?.Name);
        Assert.Equal("dbo", context.FindTable("u")?.Schema);
    }

    [Theory]
    [InlineData("SELECT * FROM dbo.|", SqlContextKind.TableSource)]
    [InlineData("SELECT * FROM a, Sales.dbo.|", SqlContextKind.TableSource)]
    [InlineData("EXEC dbo.|", SqlContextKind.Procedure)]
    [InlineData("SELECT u.|", SqlContextKind.Column)]
    [InlineData("SELECT * FROM a JOIN b ON a.|", SqlContextKind.Column)]
    public void Qualifier_ParentKind(string query, SqlContextKind expected)
    {
        var context = Analyze(query);

        Assert.Equal(SqlContextKind.MemberAccess, context.Kind);
        Assert.Equal(expected, context.MemberParentKind);
    }

    [Fact]
    public void Qualifier_MultiPart()
    {
        var context = Analyze("SELECT * FROM Sales.[dbo].|");

        Assert.Equal(SqlContextKind.MemberAccess, context.Kind);
        Assert.Equal(new[] { "Sales", "dbo" }, context.Qualifier);
    }

    [Fact]
    public void Tables_QuotedNamesAliasesAndHints()
    {
        var context = Analyze("SELECT * FROM [My Db].dbo.[Order Lines] ol WITH (NOLOCK) JOIN x.y AS z ON |");

        var lines = context.FindTable("ol");
        Assert.NotNull(lines);
        Assert.Equal("My Db", lines.Database);
        Assert.Equal("dbo", lines.Schema);
        Assert.Equal("Order Lines", lines.Name);

        Assert.Equal("y", context.FindTable("z")?.Name);
        Assert.Equal("z", context.Tables[0].Alias);
    }

    [Fact]
    public void Tables_CommaListAndSubquery()
    {
        var context = Analyze("SELECT * FROM a, b c, (SELECT 1 AS n) d WHERE |");

        Assert.Equal(new[] { "a", "b" }, context.Tables.Select(t => t.Name).Order());
        Assert.Equal("b", context.FindTable("c")?.Name);
    }

    [Fact]
    public void Tables_LimitedToCurrentBatch()
    {
        var context = Analyze("SELECT * FROM a\nGO\nSELECT * FROM b WHERE |\nGO\nSELECT * FROM c");

        Assert.Equal("b", Assert.Single(context.Tables).Name);
    }

    [Fact]
    public void Database_LastUseBeforeCaret()
    {
        var context = Analyze("USE Sales\nGO\nUSE [Stock]\nGO\nSELECT |\nGO\nUSE Other");

        Assert.Equal("Stock", context.Database);
    }

    private static SqlContext Analyze(string query)
    {
        var caret = query.IndexOf('|', StringComparison.Ordinal);
        var text = query.Remove(caret, 1);

        return SqlContextAnalyzer.Analyze(text, caret);
    }
}
