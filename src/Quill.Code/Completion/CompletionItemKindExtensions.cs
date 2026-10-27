namespace Quill.Code.Completion;

/// <summary>Regroupements de <see cref="CompletionItemKind"/> utiles à l'affichage (icônes, couleurs).</summary>
public static class CompletionItemKindExtensions
{
    /// <summary>Vrai pour une déclaration de type (classe, structure, interface, énumération, délégué, paramètre générique).</summary>
    public static bool IsType(this CompletionItemKind kind)
        => kind is CompletionItemKind.Class or CompletionItemKind.Struct or CompletionItemKind.Interface
            or CompletionItemKind.Enum or CompletionItemKind.Delegate or CompletionItemKind.TypeParameter;

    /// <summary>Vrai pour un élément appelable (méthode, fonction, procédure).</summary>
    public static bool IsCallable(this CompletionItemKind kind)
        => kind is CompletionItemKind.Method or CompletionItemKind.ExtensionMethod
            or CompletionItemKind.Function or CompletionItemKind.Procedure;
}
