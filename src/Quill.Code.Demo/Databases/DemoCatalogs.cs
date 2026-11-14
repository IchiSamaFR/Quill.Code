using System.Text;
using Quill.Code.Sql.Dialects;
using Quill.Code.Sql.MySql;
using Quill.Code.Sql.Oracle;
using Quill.Code.Sql.PostgreSql;
using Quill.Code.Sql.SqlServer.Schema;
using Quill.Data.Sql;

namespace Quill.Code.Demo.Databases;

/// <summary>
/// Fictitious catalogs of the engines without a demo database: the same shop, named and typed the way of each
/// engine (PascalCase and dbo, snake_case, UPPER CASE and SHOP schema), to show the editor without a connection.
/// </summary>
internal static class DemoCatalogs
{
    public static SqlCatalog For(SqlDialect dialect)
    {
        if (ReferenceEquals(dialect, MySqlDialect.Instance))
            return Build("shop", "shop", ToSnakeCase, "int", "varchar(100)", "decimal(10,2)", "datetime");

        if (ReferenceEquals(dialect, PostgreSqlDialect.Instance))
            return Build("shop", "public", ToSnakeCase, "integer", "text", "numeric(10,2)", "timestamp");

        if (ReferenceEquals(dialect, OracleDialect.Instance))
            return Build("ORCL", "SHOP", name => ToSnakeCase(name).ToUpperInvariant(), "NUMBER(10)", "VARCHAR2(100)", "NUMBER(10,2)", "DATE");

        return Build("Shop", "dbo", name => name, "int", "nvarchar(100)", "decimal(10,2)", "datetime2");
    }

    private static SqlCatalog Build(string database, string schema, Func<string, string> rename, string integer, string text, string money, string date)
    {
        SqlColumn Key(string name) => new(rename(name), integer) { IsNullable = false, PrimaryKeyOrdinal = 1 };
        SqlColumn Integer(string name) => new(rename(name), integer) { IsNullable = false };
        SqlColumn Text(string name, bool isNullable = false) => new(rename(name), text) { IsNullable = isNullable };
        SqlColumn Money(string name) => new(rename(name), money) { IsNullable = false };
        SqlColumn Date(string name) => new(rename(name), date) { IsNullable = false };
        SqlSchemaObject Table(string name, params SqlColumn[] columns) => new(schema, rename(name), SqlSchemaObjectKind.Table, columns);

        return new SqlCatalog(
        [
            new SqlDatabaseSchema(database,
            [
                Table("Customers", Key("Id"), Text("Name"), Text("City"), Text("Email", isNullable: true), Text("Group")),
                Table("Products", Key("Id"), Text("Name"), Text("Category"), Money("Price")),
                Table("Orders", Key("Id"), Integer("CustomerId"), Date("OrderDate"), Text("Status")),
                Table("OrderLines", Key("Id"), Integer("OrderId"), Integer("ProductId"), Integer("Quantity"), Money("UnitPrice")),
                new SqlSchemaObject(schema, rename("GetCustomerOrders"), SqlSchemaObjectKind.Procedure, []),
            ]),
        ]);
    }

    /// <summary>OrderLines → order_lines.</summary>
    private static string ToSnakeCase(string name)
    {
        var result = new StringBuilder(name.Length + 4);
        foreach (var c in name)
        {
            if (char.IsUpper(c) && result.Length > 0)
                result.Append('_');

            result.Append(char.ToLowerInvariant(c));
        }

        return result.ToString();
    }
}
