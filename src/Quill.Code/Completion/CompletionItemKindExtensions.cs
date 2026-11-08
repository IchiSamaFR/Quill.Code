namespace Quill.Code.Completion;

/// <summary>Groupings of <see cref="CompletionItemKind"/> useful for display (icons, colors).</summary>
public static class CompletionItemKindExtensions
{
    /// <summary>True for a type declaration (class, struct, interface, enum, delegate, type parameter).</summary>
    public static bool IsType(this CompletionItemKind kind)
        => kind is CompletionItemKind.Class or CompletionItemKind.Struct or CompletionItemKind.Interface
            or CompletionItemKind.Enum or CompletionItemKind.Delegate or CompletionItemKind.TypeParameter;

    /// <summary>True for a callable item (method, function, procedure).</summary>
    public static bool IsCallable(this CompletionItemKind kind)
        => kind is CompletionItemKind.Method or CompletionItemKind.ExtensionMethod
            or CompletionItemKind.Function or CompletionItemKind.Procedure;
}
