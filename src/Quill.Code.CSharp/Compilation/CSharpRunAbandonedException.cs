namespace Quill.Code.CSharp.Compilation;

/// <summary>
/// Cancellation requested while the compiled code is still running: it cannot be interrupted in-process
/// and finishes in the background (see <see cref="CSharpCompilationResult.RunAsync{T}"/>).
/// </summary>
public sealed class CSharpRunAbandonedException : OperationCanceledException
{
    /// <summary>Execution abandoned on cancellation of <paramref name="cancellationToken"/>.</summary>
    public CSharpRunAbandonedException(CancellationToken cancellationToken)
        : base("Le code ne peut pas être interrompu : il se termine en arrière-plan.", null, cancellationToken)
    {
    }
}
