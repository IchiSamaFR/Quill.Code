namespace Quill.Code.CSharp.Compilation;

/// <summary>
/// Annulation demandée alors que le code compilé tourne encore : il ne peut pas être interrompu dans le processus
/// et se termine en arrière-plan (voir <see cref="CSharpCompilationResult.RunAsync{T}"/>).
/// </summary>
public sealed class CSharpRunAbandonedException : OperationCanceledException
{
    /// <summary>Exécution abandonnée sur l'annulation de <paramref name="cancellationToken"/>.</summary>
    public CSharpRunAbandonedException(CancellationToken cancellationToken)
        : base("Le code ne peut pas être interrompu : il se termine en arrière-plan.", null, cancellationToken)
    {
    }
}
