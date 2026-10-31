namespace Quill.Code.CSharp.Linq.Modeling;

/// <summary>Code C# d'un DbContext genere depuis la structure d'une base.</summary>
/// <param name="Namespace">Espace de noms du code genere.</param>
/// <param name="ContextClassName">Classe du DbContext (ex. RaveOnDbContext).</param>
/// <param name="Source">Code C# complet, compilable seul avec EF Core.</param>
/// <param name="EntityClassNames">Classes generees, dans l'ordre du source.</param>
internal sealed record EfGeneratedModel(
    string Namespace,
    string ContextClassName,
    string Source,
    IReadOnlyList<string> EntityClassNames);
