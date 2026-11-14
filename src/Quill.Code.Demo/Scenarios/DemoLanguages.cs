using Quill.Code.CSharp.Linq;
using Quill.Code.Demo.Constants;
using Quill.Code.Demo.Databases;
using Quill.Code.Sql.SqlServer;

namespace Quill.Code.Demo.Scenarios;

/// <summary>Quill editor service of a scenario: that is all an application has to instantiate.</summary>
internal static class DemoLanguages
{
    public static ICodeLanguageService Create(DemoScenario scenario, DemoDatabase database)
    {
        if (scenario.Dialect is not { } dialect)
        {
            return new LinqLanguageService(new LinqLanguageOptions
            {
                Provider = database.LinqProvider,
                Schema = _ => Task.FromResult(database.Schema),
            });
        }

        return new SqlLanguageService(new SqlLanguageOptions
        {
            Dialect = dialect,
            Catalog = scenario.CanRun ? database.Catalog : DemoCatalogs.For(dialect),
            Variables = [new SqlVariable(DemoConstants.ParameterName, "int", "Demo parameter")],
            // Syntax checking (ScriptDom) only exists for T-SQL.
            CheckSyntax = ReferenceEquals(dialect, SqlServerDialect.Instance),
        });
    }
}
