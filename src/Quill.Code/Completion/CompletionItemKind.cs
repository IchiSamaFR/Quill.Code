namespace Quill.Code.Completion;

/// <summary>Nature d'une proposition ; l'ordre sert aussi de priorité d'affichage à score égal.</summary>
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

    /// <summary>Mot déjà présent dans le document.</summary>
    Text,
}
