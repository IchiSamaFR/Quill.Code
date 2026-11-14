namespace Quill.Code.Demo.Constants;

/// <summary>Settings of the demo application.</summary>
internal static class DemoConstants
{
    /// <summary>Folder of the demo SQLite database, under the temporary folder.</summary>
    internal const string DatabaseFolder = "QuillCodeDemo";

    internal const string DatabaseFile = "shop.db";

    /// <summary>Script that (re)creates the database: tables and fake data.</summary>
    internal const string DatabaseScript = "Shop.sqlite.sql";

    internal const string SqlHighlighting = "Sql.xshd";

    internal const string LinqHighlighting = "Linq.xshd";

    /// <summary>Parameter provided to the SQL editor: written @MaxRows or :MaxRows depending on the dialect.</summary>
    internal const string ParameterName = "MaxRows";

    /// <summary>Document page width: wide enough for a line never to wrap.</summary>
    internal const double EditorPageWidth = 4000;

    internal const int HighlightDelayMilliseconds = 150;

    internal const int DiagnosticDelayMilliseconds = 600;

    internal const int HoverDelayMilliseconds = 350;

    /// <summary>Tag of the result tabs, removed on each execution.</summary>
    internal const string ResultTabTag = "result";
}
