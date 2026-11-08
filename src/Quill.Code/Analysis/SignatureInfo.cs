namespace Quill.Code.Analysis;

/// <summary>One overload: <see cref="Prefix"/> + parameters separated by ", " + <see cref="Suffix"/>.</summary>
/// <param name="Prefix">E.g. "IQueryable&lt;Student&gt; Where(".</param>
/// <param name="Parameters">E.g. "Expression&lt;Func&lt;Student, bool&gt;&gt; predicate".</param>
/// <param name="Suffix">E.g. ")".</param>
/// <param name="Description">Optional detail (type declaring the method, documentation).</param>
public sealed record SignatureInfo(string Prefix, IReadOnlyList<string> Parameters, string Suffix, string? Description = null);
