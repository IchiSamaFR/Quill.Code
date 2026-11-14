using Quill.Code.Sql.MySql;
using Quill.Code.Sql.Oracle;
using Quill.Code.Sql.PostgreSql;
using Quill.Code.Sql.Sqlite;
using Quill.Code.Sql.SqlServer;

namespace Quill.Code.Tests.Sql;

public class SqlDialectTests
{
    [Fact]
    public void QuoteIdentifier_UsesTheDelimitersOfTheEngine()
    {
        Assert.Equal("[Ma Table]", SqlServerDialect.Instance.QuoteIdentifier("Ma Table"));
        Assert.Equal("`Ma Table`", MySqlDialect.Instance.QuoteIdentifier("Ma Table"));
        Assert.Equal("\"Ma Table\"", PostgreSqlDialect.Instance.QuoteIdentifier("Ma Table"));
        Assert.Equal("\"Ma Table\"", OracleDialect.Instance.QuoteIdentifier("Ma Table"));
        Assert.Equal("\"Ma Table\"", SqliteDialect.Instance.QuoteIdentifier("Ma Table"));
    }

    [Fact]
    public void QuoteIdentifier_QuotesReservedWordsOnly()
    {
        Assert.Equal("Users", SqlServerDialect.Instance.QuoteIdentifier("Users"));
        Assert.Equal("[Order]", SqlServerDialect.Instance.QuoteIdentifier("Order"));
        Assert.Equal("`order`", MySqlDialect.Instance.QuoteIdentifier("order"));
        Assert.Equal("[a]]b]", SqlServerDialect.Instance.QuoteIdentifier("a]b"));
    }

    [Fact]
    public void QuoteIdentifier_KeepsTheCaseTheEngineWouldFold()
    {
        Assert.Equal("EMPLOYEES", OracleDialect.Instance.QuoteIdentifier("EMPLOYEES"));
        Assert.Equal("\"Employees\"", OracleDialect.Instance.QuoteIdentifier("Employees"));
        Assert.Equal("users", PostgreSqlDialect.Instance.QuoteIdentifier("users"));
        Assert.Equal("\"Users\"", PostgreSqlDialect.Instance.QuoteIdentifier("Users"));
    }

    [Fact]
    public void FormatParameter_UsesThePrefixOfTheEngine()
    {
        Assert.Equal("@UserId", SqlServerDialect.Instance.FormatParameter("UserId"));
        Assert.Equal(":UserId", OracleDialect.Instance.FormatParameter("@UserId"));
        Assert.Equal("@UserId", PostgreSqlDialect.Instance.FormatParameter(":UserId"));
    }

    [Fact]
    public void SelectFirstRows_UsesTheRowLimitOfTheEngine()
    {
        var sqlServer = SqlServerDialect.Instance;
        Assert.Equal(
            "SELECT TOP (100) *\nFROM [Shop].[dbo].[Users];",
            sqlServer.SelectFirstRows(sqlServer.FormatObjectName("Shop", "dbo", "Users", alwaysQuote: true), 100));

        var mySql = MySqlDialect.Instance;
        Assert.Equal(
            "SELECT *\nFROM `shop`.`users`\nLIMIT 100;",
            mySql.SelectFirstRows(mySql.FormatObjectName("shop", "shop", "users", alwaysQuote: true), 100));

        var oracle = OracleDialect.Instance;
        Assert.Equal(
            "SELECT *\nFROM \"HR\".\"EMPLOYEES\"\nFETCH FIRST 1 ROWS ONLY;",
            oracle.SelectFirstRows(oracle.FormatObjectName("ORCL", "HR", "EMPLOYEES", alwaysQuote: true), 1));

        var postgreSql = PostgreSqlDialect.Instance;
        Assert.Equal(
            "SELECT *\nFROM public.users\nLIMIT 10;",
            postgreSql.SelectFirstRows(postgreSql.FormatObjectName("shop", "public", "users"), 10));
    }
}
