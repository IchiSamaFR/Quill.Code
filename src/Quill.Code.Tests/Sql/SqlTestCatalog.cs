using Quill.Code.Sql.Schema;

namespace Quill.Code.Tests.Sql;

/// <summary>Catalogue en mémoire des tests : base Shop par défaut, base Archive à côté.</summary>
internal static class SqlTestCatalog
{
    public static SqlCatalog Create() => new(
    [
        new SqlDatabaseSchema("Shop",
        [
            new SqlSchemaObject("dbo", "Users", SqlSchemaObjectKind.Table,
            [
                new SqlColumn("Id", "int", IsNullable: false, IsPrimaryKey: true),
                new SqlColumn("Email", "nvarchar(200)"),
                new SqlColumn("Order", "int"),
            ]),
            new SqlSchemaObject("sales", "Orders", SqlSchemaObjectKind.Table,
            [
                new SqlColumn("Id", "int", IsNullable: false, IsPrimaryKey: true),
                new SqlColumn("UserId", "int", IsNullable: false),
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
