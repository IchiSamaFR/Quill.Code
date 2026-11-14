using Quill.Code.Sql.Dialects;

namespace Quill.Code.Demo.Scenarios;

/// <summary>A demo scenario: a language, its highlighting, its sample text.</summary>
/// <param name="Name">Label in the list.</param>
/// <param name="SampleResource">Text loaded into the editor.</param>
/// <param name="HighlightingResource">Highlighting .xshd file.</param>
/// <param name="Dialect">SQL dialect; null for LINQ.</param>
/// <param name="CanRun">True on the demo database, false on a fictitious catalog (editor only).</param>
internal sealed record DemoScenario(string Name, string SampleResource, string HighlightingResource, SqlDialect? Dialect, bool CanRun)
{
    public bool IsLinq => Dialect is null;
}
