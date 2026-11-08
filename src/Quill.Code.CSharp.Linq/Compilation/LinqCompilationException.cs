namespace Quill.Code.CSharp.Linq.Compilation;

/// <summary>The generated code does not compile (generator bug, unforeseen schema case), or a parameter does not fit its type.</summary>
internal sealed class LinqCompilationException : Exception
{
    public LinqCompilationException(string message, IReadOnlyList<string> errors)
        : base(message)
    {
        Errors = errors;
    }

    /// <summary>Roslyn messages, positions in the generated source; empty for a parameter.</summary>
    public IReadOnlyList<string> Errors { get; }
}
