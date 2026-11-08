namespace Quill.Code.Completion;

/// <summary>Kind of a suggestion; the order also sets display priority for equal scores.</summary>
public enum CompletionItemKind
{
    // Database objects
    Column,
    Table,
    View,
    Procedure,
    Schema,
    Database,

    // C# members and symbols
    Property,
    Field,
    Method,
    ExtensionMethod,
    Event,
    Variable,
    Parameter,
    Constant,
    EnumMember,

    // C# types
    Class,
    Struct,
    Interface,
    Enum,
    Delegate,
    TypeParameter,
    Namespace,

    Function,
    Keyword,
    DataType,
    Snippet,

    /// <summary>Word already present in the document.</summary>
    Text,
}
