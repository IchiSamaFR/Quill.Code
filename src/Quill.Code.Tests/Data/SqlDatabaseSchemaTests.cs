using Quill.Data.Sql;

namespace Quill.Code.Tests.Data;

public sealed class SqlDatabaseSchemaTests
{
    private static readonly SqlDatabaseSchema Schema = new("Shop",
    [
        new SqlSchemaObject("sales", "Users", SqlSchemaObjectKind.Table, []),
        new SqlSchemaObject("dbo", "Users", SqlSchemaObjectKind.Table, []),
        new SqlSchemaObject("Sales", "Orders", SqlSchemaObjectKind.View, []),
    ]);

    [Fact]
    public void Find_PrefersDefaultSchemaWithoutQualifier()
    {
        Assert.Equal("dbo", Schema.Find(null, "users")?.Schema);
        Assert.Equal("sales", Schema.Find("SALES", "Users")?.Schema);
        Assert.Null(Schema.Find("hr", "Users"));
    }

    [Fact]
    public void Schemas_AreDistinctAndSorted()
    {
        Assert.Equal(["dbo", "sales"], Schema.Schemas);
        Assert.True(Schema.HasSchema("SALES"));
    }

    [Fact]
    public void Column_DescriptionLikeSsms()
    {
        var column = new SqlColumn("Id", "int") { IsNullable = false, PrimaryKeyOrdinal = 1, IsIdentity = true };

        Assert.True(column.IsPrimaryKey);
        Assert.Equal("PK, int, identity, not null", column.Description);
    }
}
