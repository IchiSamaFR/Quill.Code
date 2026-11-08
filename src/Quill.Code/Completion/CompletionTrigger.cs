namespace Quill.Code.Completion;

/// <summary>What triggered the completion list.</summary>
public enum CompletionTrigger
{
    /// <summary>Typing an identifier character.</summary>
    Typing,

    /// <summary>Explicit request (Ctrl+Space).</summary>
    Explicit,

    /// <summary>Typing a dot (object.member, schema.table).</summary>
    MemberAccess,
}
