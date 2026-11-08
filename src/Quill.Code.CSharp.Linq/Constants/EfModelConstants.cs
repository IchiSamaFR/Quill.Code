namespace Quill.Code.CSharp.Linq.Constants;

/// <summary>Names of the EF model generated from a database structure.</summary>
internal static class EfModelConstants
{
    /// <summary>Namespace of the generated DbContext and entities.</summary>
    internal const string GeneratedNamespace = "Quill.Generated";

    /// <summary>Generated class holding Dump(); its private property <see cref="DumpSinkPropertyName"/> receives the values.</summary>
    internal const string ExtensionsClassName = "QueryExtensions";

    /// <inheritdoc cref="ExtensionsClassName"/>
    internal const string DumpSinkPropertyName = "DumpSink";
}
