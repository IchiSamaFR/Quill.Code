using Quill.Code.Sql.SqlServer.Schema;
using Quill.Data.Sql;

namespace Quill.Code.Tests.SqlServer;

/// <summary>In-memory test catalog: Shop database by default, Archive database alongside.</summary>
internal static class SqlTestCatalog
{
    public static SqlCatalog Create() => new(
    [
        new SqlDatabaseSchema("Shop",
        [
            new SqlSchemaObject("dbo", "Users", SqlSchemaObjectKind.Table,
            [
                new SqlColumn("Id", "int") { IsNullable = false, PrimaryKeyOrdinal = 1 },
                new SqlColumn("Email", "nvarchar(200)"),
                new SqlColumn("Order", "int"),
            ]),
            new SqlSchemaObject("sales", "Orders", SqlSchemaObjectKind.Table,
            [
                new SqlColumn("Id", "int") { IsNullable = false, PrimaryKeyOrdinal = 1 },
                new SqlColumn("UserId", "int") { IsNullable = false },
            ]),
            new SqlSchemaObject("dbo", "ActiveUsers", SqlSchemaObjectKind.View, [new SqlColumn("Id", "int")]),
            new SqlSchemaObject("dbo", "PurgeUsers", SqlSchemaObjectKind.Procedure, []),
        ]),
        new SqlDatabaseSchema("Archive",
        [
            new SqlSchemaObject("dbo", "OldUsers", SqlSchemaObjectKind.Table, [new SqlColumn("Id", "int")]),
        ]),
    ]);
}
