namespace Quill.Code.CSharp.Linq.Constants;

/// <summary>Limits and labels of the LINQ execution.</summary>
internal static class LinqExecutionConstants
{
    /// <summary>Log output of a script: a display loop must not saturate memory or the Messages tab.</summary>
    internal const int MaxLogLength = 200_000;

    /// <summary>Errors reported when the generated code (model, parameters) does not compile.</summary>
    internal const int MaxGeneratedErrors = 20;

    /// <summary>Compilation errors included in the exception of a read (ReadAsync).</summary>
    internal const int MaxReadErrors = 5;

    /// <summary>Displayed bytes of a binary value, like the SQL execution.</summary>
    internal const int MaxBinaryBytes = 64;

    /// <summary>Column of a Dump of simple values.</summary>
    internal const string ScalarColumn = "Value";
}
