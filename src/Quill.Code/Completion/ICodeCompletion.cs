
namespace Quill.Code.Completion;

/// <summary>Completion source of an editor, on raw text: decouples the control from the engine (Roslyn…).</summary>
public interface ICodeCompletion
{
    /// <summary>Suggestions valid at <paramref name="position"/> in <paramref name="code"/>.</summary>
    Task<CompletionList> GetCompletionsAsync(string code, int position, CancellationToken cancellationToken = default);

    /// <summary>Change to apply to <paramref name="code"/> to commit <paramref name="item"/>.</summary>
    Task<CompletionChange> GetChangeAsync(string code, CompletionItem item, CancellationToken cancellationToken = default);
}
