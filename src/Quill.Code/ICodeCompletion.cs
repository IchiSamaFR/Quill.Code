using Quill.Code.Completion;

namespace Quill.Code;

/// <summary>Source d'autocomplétion d'un éditeur, sur le texte brut : découple le contrôle du moteur (Roslyn…).</summary>
public interface ICodeCompletion
{
    /// <summary>Propositions valables à <paramref name="position"/> dans <paramref name="code"/>.</summary>
    Task<CompletionList> GetCompletionsAsync(string code, int position, CancellationToken cancellationToken = default);

    /// <summary>Modification à appliquer à <paramref name="code"/> pour valider <paramref name="item"/>.</summary>
    Task<CompletionChange> GetChangeAsync(string code, CompletionItem item, CancellationToken cancellationToken = default);
}
