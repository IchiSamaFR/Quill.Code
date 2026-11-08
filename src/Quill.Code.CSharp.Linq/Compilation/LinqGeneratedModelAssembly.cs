using Microsoft.CodeAnalysis;
using Quill.Code.CSharp.Linq.Modeling;
using System.Reflection;

namespace Quill.Code.CSharp.Linq.Compilation;

/// <summary>Compiled and loaded model, reusable by all queries on this schema.</summary>
internal sealed class LinqGeneratedModelAssembly
{
    public required EfGeneratedModel Model { get; init; }

    /// <summary>Engine of the model: connection of the EF context.</summary>
    public required ILinqDatabaseProvider Provider { get; init; }

    /// <summary>Platform, EF and provider references, shared by the model and its queries.</summary>
    public required IReadOnlyList<MetadataReference> PlatformReferences { get; init; }

    public required Assembly Assembly { get; init; }

    public required Type ContextType { get; init; }

    /// <summary>Destination of the current query's Dump() calls (generated static property, specific to each execution).</summary>
    public required PropertyInfo DumpSinkProperty { get; init; }

    /// <summary>Roslyn reference to the compiled image, to compile queries against it.</summary>
    public required MetadataReference Reference { get; init; }
}
