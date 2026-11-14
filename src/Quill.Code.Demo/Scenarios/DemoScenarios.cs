using Quill.Code.Demo.Constants;
using Quill.Code.Sql.MySql;
using Quill.Code.Sql.Oracle;
using Quill.Code.Sql.PostgreSql;
using Quill.Code.Sql.Sqlite;
using Quill.Code.Sql.SqlServer;

namespace Quill.Code.Demo.Scenarios;

/// <summary>Scenarios offered by the demo.</summary>
internal static class DemoScenarios
{
    public static IReadOnlyList<DemoScenario> All { get; } =
    [
        new("SQL · SQLite — demo database, runnable", "Sample.Sqlite.sql", DemoConstants.SqlHighlighting, SqliteDialect.Instance, CanRun: true),
        new("LINQ · SQLite — demo database, runnable", "Sample.Linq.linq", DemoConstants.LinqHighlighting, Dialect: null, CanRun: true),
        new("SQL · SQL Server — fictitious catalog", "Sample.SqlServer.sql", DemoConstants.SqlHighlighting, SqlServerDialect.Instance, CanRun: false),
        new("SQL · MySQL — fictitious catalog", "Sample.MySql.sql", DemoConstants.SqlHighlighting, MySqlDialect.Instance, CanRun: false),
        new("SQL · PostgreSQL — fictitious catalog", "Sample.PostgreSql.sql", DemoConstants.SqlHighlighting, PostgreSqlDialect.Instance, CanRun: false),
        new("SQL · Oracle — fictitious catalog", "Sample.Oracle.sql", DemoConstants.SqlHighlighting, OracleDialect.Instance, CanRun: false),
    ];
}
