namespace Quill.Code.Constants;

/// <summary>Limits shared by query executors.</summary>
public static class QueryExecutionLimits
{
    /// <summary>Limit per result set, so as not to saturate memory or the grid.</summary>
    public const int MaxRowsPerResultSet = 10_000;

    /// <summary>Connection timeout for a parameterized query (maps, charts): an unreachable server must not freeze the display.</summary>
    public const int ReadConnectTimeoutSeconds = 8;

    /// <summary>Execution timeout for a parameterized query.</summary>
    public const int ReadCommandTimeoutSeconds = 120;
}
