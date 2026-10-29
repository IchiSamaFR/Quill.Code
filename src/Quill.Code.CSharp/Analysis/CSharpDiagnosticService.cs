using Quill.Code.Analysis;
using Quill.Code.CSharp.Workspaces;

namespace Quill.Code.CSharp.Analysis;

/// <summary>Diagnostics C# fournis par Roslyn.</summary>
/// <remarks>
/// Les méthodes qui prennent le code créent le document depuis le projet (transformation, références du texte) ;
/// celles qui prennent un document travaillent sur n'importe quel projet. Roslyn calcule en partie de façon
/// synchrone : chaque demande part sur le pool de threads pour ne pas figer l'interface.
/// </remarks>
/// <param name="project">Projet des demandes sur le code.</param>
public sealed class CSharpDiagnosticService(CSharpProject project) : ICodeDiagnostic
{
    private readonly CSharpProject _project = project ?? throw new ArgumentNullException(nameof(project));

    /// <inheritdoc />
    public Task<IReadOnlyList<CodeDiagnostic>> GetDiagnosticsAsync(string code, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(code);
        return Task.Run(() => CSharpDiagnosticEngine.GetDiagnosticsAsync(_project.CreateDocument(code), cancellationToken), cancellationToken);
    }

    /// <summary>Erreurs, avertissements et informations de <paramref name="document"/>.</summary>
    public Task<IReadOnlyList<CodeDiagnostic>> GetDiagnosticsAsync(CSharpDocument document, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);
        return Task.Run(() => CSharpDiagnosticEngine.GetDiagnosticsAsync(document, cancellationToken), cancellationToken);
    }
}
