using Quill.Code.Sql.SqlServer.Execution;
using Xunit;

namespace Quill.Code.Tests.SqlServer;

public class SqlBatchSplitterTests
{
    [Fact]
    public void Split_OnGo_NumbersBatchesFromTheirFirstLine()
    {
        var batches = SqlBatchSplitter.Split("SELECT 1\nGO\nSELECT 2");

        Assert.Collection(batches,
            b => Assert.Equal(new SqlBatch("SELECT 1\n", 1, 1), b),
            b => Assert.Equal(new SqlBatch("SELECT 2\n", 3, 1), b));
    }

    [Fact]
    public void Split_GoWithCount_RepeatsPreviousBatch()
    {
        var batch = Assert.Single(SqlBatchSplitter.Split("SELECT 1\nGO 3"));

        Assert.Equal(3, batch.RepeatCount);
    }

    [Theory]
    [InlineData("SELECT 'a\nGO\nb'")]
    [InlineData("/*\nGO\n*/\nSELECT 1")]
    [InlineData("SELECT 1 -- GO\nSELECT 2")]
    [InlineData("GOTO fin\nfin: SELECT 1")]
    public void Split_GoInsideLiteralCommentOrKeyword_IsNotASeparator(string script)
        => Assert.Single(SqlBatchSplitter.Split(script));

    [Theory]
    [InlineData("select 1\n  go  -- fin\nselect 2")]
    [InlineData("SELECT 1\r\nGO\r\nSELECT 2")]
    public void Split_GoLineVariants_AreSeparators(string script)
        => Assert.Equal(2, SqlBatchSplitter.Split(script).Count);

    [Fact]
    public void Split_OnlySeparators_ReturnsNoBatch()
        => Assert.Empty(SqlBatchSplitter.Split("GO\nGO"));
}
