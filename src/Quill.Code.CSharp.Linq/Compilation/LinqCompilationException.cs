namespace Quill.Code.CSharp.Linq.Compilation;

/// <summary>Le code genere ne compile pas (bug du generateur, cas de schema non prevu), ou un parametre ne convient pas a son type.</summary>
internal sealed class LinqCompilationException : Exception
{
    public LinqCompilationException(string message, IReadOnlyList<string> errors)
        : base(message)
    {
        Errors = errors;
    }

    /// <summary>Messages Roslyn, positions dans le source genere ; vide pour un parametre.</summary>
    public IReadOnlyList<string> Errors { get; }
}
