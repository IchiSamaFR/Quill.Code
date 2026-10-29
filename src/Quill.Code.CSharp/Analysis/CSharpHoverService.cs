using Quill.Code.Analysis;
using Quill.Code.CSharp.Workspaces;

namespace Quill.Code.CSharp.Analysis;

/// <summary>Infobulles C# fournies par Roslyn.</summary>
/// <remarks>
/// Les méthodes qui prennent le code créent le document depuis le projet (transformation, références du texte) ;
/// celles qui prennent un document travaillent sur n'importe quel projet. Roslyn calcule en partie de façon
/// synchrone : chaque demande part sur le pool de threads pour ne pas figer l'interface.
/// </remarks>
/// <param name="project">Projet des demandes sur le code.</param>
public sealed class CSharpHoverService(CSharpProject project) : ICodeHover
{
    private readonly CSharpProject _project = project ?? throw new ArgumentNullException(nameof(project));

    /// <inheritdoc />
    public Task<HoverInfo?> GetHoverAsync(string code, int position, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(code);
        return Task.Run(() => CSharpHoverEngine.GetHoverAsync(_project.CreateDocument(code), position, cancellationToken), cancellationToken);
    }

    /// <summary>Infobulle du nom à <paramref name="position"/> dans <paramref name="document"/>, null si elle ne désigne rien de connu.</summary>
    public Task<HoverInfo?> GetHoverAsync(CSharpDocument document, int position, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);
        return Task.Run(() => CSharpHoverEngine.GetHoverAsync(document, position, cancellationToken), cancellationToken);
    }
}
