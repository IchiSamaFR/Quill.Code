using Quill.Code.Analysis;
using Quill.Code.Completion;
using Quill.Code.CSharp.Workspaces;

namespace Quill.Code.CSharp;

/// <summary>
/// Services d'éditeur C# fournis par Roslyn. Les positions reçues et rendues sont celles du texte de l'éditeur
/// (<see cref="CSharpDocument.Text"/>), jamais celles du source généré. Les méthodes qui prennent le code travaillent sur
/// le <see cref="Project"/> du service ; celles qui prennent un document, sur n'importe quel projet.
/// </summary>
public interface ICSharpLanguageService : ICodeCompletionProvider, ICodeDiagnosticProvider, IDisposable
{
    /// <summary>Projet du service, créé à la première demande : utilisé par les méthodes qui prennent le code.</summary>
    CSharpProject Project { get; }

    /// <inheritdoc cref="WarmUpAsync(CSharpDocument, CancellationToken)"/>
    /// <param name="code">Texte de préchauffage ; « Console. » prépare aussi la recherche des membres.</param>
    Task WarmUpAsync(string code = "", CancellationToken cancellationToken = default);

    /// <inheritdoc cref="GetHoverAsync(CSharpDocument, int, CancellationToken)"/>
    Task<HoverInfo?> GetHoverAsync(string code, int position, CancellationToken cancellationToken = default);

    /// <inheritdoc cref="GetSignatureHelpAsync(CSharpDocument, int, CancellationToken)"/>
    Task<SignatureHelpInfo?> GetSignatureHelpAsync(string code, int position, CancellationToken cancellationToken = default);

    /// <summary>Précharge Roslyn (composition MEF, métadonnées, JIT) : sinon la première demande prend une à deux secondes.</summary>
    Task WarmUpAsync(CSharpDocument document, CancellationToken cancellationToken = default);

    /// <summary>Propositions valables à <paramref name="position"/>.</summary>
    Task<CompletionList> GetCompletionsAsync(CSharpDocument document, int position, CancellationToken cancellationToken = default);

    /// <summary>Modification à appliquer pour valider <paramref name="item"/>, issue de <see cref="GetCompletionsAsync"/>.</summary>
    /// <param name="document">Document du texte courant (peut différer de celui de la demande de propositions).</param>
    Task<CompletionChange> GetChangeAsync(CSharpDocument document, CompletionItem item, CancellationToken cancellationToken = default);

    /// <summary>Erreurs, avertissements et informations du texte de l'éditeur (100 au plus).</summary>
    Task<IReadOnlyList<CodeDiagnostic>> GetDiagnosticsAsync(CSharpDocument document, CancellationToken cancellationToken = default);

    /// <summary>Infobulle du nom à <paramref name="position"/>, null si elle ne désigne rien de connu.</summary>
    Task<HoverInfo?> GetHoverAsync(CSharpDocument document, int position, CancellationToken cancellationToken = default);

    /// <summary>Surcharges de la méthode appelée à <paramref name="position"/>, null hors d'une liste d'arguments.</summary>
    Task<SignatureHelpInfo?> GetSignatureHelpAsync(CSharpDocument document, int position, CancellationToken cancellationToken = default);
}
