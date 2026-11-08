namespace Quill.Code.CSharp.Linq.Modeling;

/// <summary>C# code of a DbContext generated from a database structure.</summary>
/// <param name="Namespace">Namespace of the generated code.</param>
/// <param name="ContextClassName">DbContext class (e.g. RaveOnDbContext).</param>
/// <param name="Source">Complete C# code, compilable on its own with EF Core.</param>
/// <param name="EntityClassNames">Generated classes, in source order.</param>
internal sealed record EfGeneratedModel(
    string Namespace,
    string ContextClassName,
    string Source,
    IReadOnlyList<string> EntityClassNames);
