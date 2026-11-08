using Microsoft.CodeAnalysis;
using System.Reflection;

namespace Quill.Code.CSharp.Linq.Compilation;

/// <summary>
/// Compiled and loaded QueryGlobals: the context (Db) and one member per query parameter, typed but without value.
/// Reusable by all queries of the same model and the same set of parameters (names and types); values
/// are set on each execution (AsyncLocal properties, specific to each flow).
/// </summary>
internal sealed class LinqParametersAssembly
{
    /// <summary>Model QueryGlobals is compiled against; null: C# only (Db is a DbContext).</summary>
    public LinqGeneratedModelAssembly? Model { get; init; }

    public required Assembly Assembly { get; init; }

    /// <summary>Db property of QueryGlobals, set before each execution.</summary>
    public required PropertyInfo ContextProperty { get; init; }

    /// <summary>Parameter properties, by exposed name (without @), set before each execution.</summary>
    public required IReadOnlyDictionary<string, PropertyInfo> ParameterProperties { get; init; }

    /// <summary>Roslyn reference to the compiled image, to compile queries against it.</summary>
    public required MetadataReference Reference { get; init; }
}
