namespace Quill.Code.Completion;

/// <summary>Kind of a suggestion; the order also sets display priority for equal scores.</summary>
public enum CompletionItemKind
{
    // Objets de base de données
    Column,
    Table,
    View,
    Procedure,
    Schema,
    Database,

    // Membres et symboles C#
    Property,
    Field,
    Method,
    ExtensionMethod,
    Event,
    Variable,
    Parameter,
    Constant,
    EnumMember,

    // Types C#
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
