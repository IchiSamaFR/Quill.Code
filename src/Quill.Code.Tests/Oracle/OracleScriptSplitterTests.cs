using Quill.Code.Execution;
using Quill.Code.Sql.Oracle.Execution;
using Xunit;

namespace Quill.Code.Tests.Oracle;

public class OracleScriptSplitterTests
{
    [Fact]
    public void Split_OnSemicolon_RemovesTerminatorAndNumbersLines()
    {
        var batches = OracleScriptSplitter.Split("SELECT 1 FROM DUAL;\n\nSELECT 2 FROM DUAL;");

        Assert.Collection(batches,
            b => Assert.Equal(new QueryBatch("SELECT 1 FROM DUAL", 1), b),
            b => Assert.Equal(new QueryBatch("SELECT 2 FROM DUAL", 3), b));
    }

    [Fact]
    public void Split_PlSqlBlock_KeepsInnerSemicolonsUntilSlash()
    {
        var batches = OracleScriptSplitter.Split("BEGIN\n  NULL;\n  NULL;\nEND;\n/\nSELECT 1 FROM DUAL");

        Assert.Collection(batches,
            b => Assert.Equal(new QueryBatch("BEGIN\n  NULL;\n  NULL;\nEND;", 1), b),
            b => Assert.Equal(new QueryBatch("SELECT 1 FROM DUAL", 6), b));
    }

    [Theory]
    [InlineData("CREATE OR REPLACE PROCEDURE p IS BEGIN NULL; END;\n/")]
    [InlineData("CREATE OR REPLACE EDITIONABLE PACKAGE BODY pk AS END;\n/")]
    [InlineData("DECLARE x NUMBER; BEGIN x := 4 / 2; END;\n/")]
    public void Split_CreatePlSqlObject_IsOneBatch(string script)
    {
        var batch = Assert.Single(OracleScriptSplitter.Split(script));

        Assert.EndsWith("END;", batch.Text, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("SELECT 'a;b' FROM DUAL")]
    [InlineData("SELECT q'[a;b]' FROM DUAL")]
    [InlineData("SELECT \"A;B\" FROM T")]
    [InlineData("SELECT 1 /* ; */ FROM DUAL")]
    [InlineData("SELECT 1 -- ;\nFROM DUAL")]
    public void Split_SemicolonInLiteralOrComment_DoesNotSplit(string script)
    {
        Assert.Single(OracleScriptSplitter.Split(script));
    }

    [Fact]
    public void Split_CommentsAndSlashOnly_HasNoBatch()
    {
        Assert.Empty(OracleScriptSplitter.Split("-- rien\n/* toujours rien */\n/\n"));
    }

    [Fact]
    public void Split_SlashAfterStatement_IsIgnored()
    {
        var batches = OracleScriptSplitter.Split("UPDATE t SET a = 1;\n/\nCOMMIT;");

        Assert.Collection(batches,
            b => Assert.Equal("UPDATE t SET a = 1", b.Text),
            b => Assert.Equal(new QueryBatch("COMMIT", 3), b));
    }
}
