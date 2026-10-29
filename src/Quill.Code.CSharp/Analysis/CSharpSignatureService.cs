using Quill.Code.Analysis;
using Quill.Code.CSharp.Workspaces;

namespace Quill.Code.CSharp.Analysis;

/// <summary>Aide à la signature C# fournie par Roslyn.</summary>
/// <remarks>
/// Les méthodes qui prennent le code créent le document depuis le projet (transformation, références du texte) ;
/// celles qui prennent un document travaillent sur n'importe quel projet. Roslyn calcule en partie de façon
/// synchrone : chaque demande part sur le pool de threads pour ne pas figer l'interface.
/// </remarks>
/// <param name="project">Projet des demandes sur le code.</param>
public sealed class CSharpSignatureService(CSharpProject project) : ICodeSignature
{
    private readonly CSharpProject _project = project ?? throw new ArgumentNullException(nameof(project));

    /// <inheritdoc />
    public Task<SignatureHelpInfo?> GetSignatureHelpAsync(string code, int position, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(code);
        return Task.Run(() => CSharpSignatureHelpEngine.GetSignatureHelpAsync(_project.CreateDocument(code), position, cancellationToken), cancellationToken);
    }

    /// <summary>Surcharges de la méthode appelée à <paramref name="position"/> dans <paramref name="document"/>, null hors d'une liste d'arguments.</summary>
    public Task<SignatureHelpInfo?> GetSignatureHelpAsync(CSharpDocument document, int position, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);
        return Task.Run(() => CSharpSignatureHelpEngine.GetSignatureHelpAsync(document, position, cancellationToken), cancellationToken);
    }
}
