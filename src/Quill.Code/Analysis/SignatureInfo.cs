namespace Quill.Code.Analysis;

/// <summary>Une surcharge : <see cref="Prefix"/> + paramètres séparés par ", " + <see cref="Suffix"/>.</summary>
/// <param name="Prefix">Ex. "IQueryable&lt;Student&gt; Where(".</param>
/// <param name="Parameters">Ex. "Expression&lt;Func&lt;Student, bool&gt;&gt; predicate".</param>
/// <param name="Suffix">Ex. ")".</param>
/// <param name="Description">Précision facultative (type qui déclare la méthode, documentation).</param>
public sealed record SignatureInfo(string Prefix, IReadOnlyList<string> Parameters, string Suffix, string? Description = null);
