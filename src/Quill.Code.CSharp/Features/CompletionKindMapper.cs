using Microsoft.CodeAnalysis.Tags;
using Quill.Code.Completion;
using System.Collections.Immutable;

namespace Quill.Code.CSharp.Features;

/// <summary>Kind of a Roslyn suggestion based on its tags.</summary>
internal static class CompletionKindMapper
{
    // Du plus précis au plus général : une méthode d'extension porte aussi l'étiquette « Method ».
    private static readonly (string Tag, CompletionItemKind Kind)[] Kinds =
    [
        (WellKnownTags.ExtensionMethod, CompletionItemKind.ExtensionMethod),
        (WellKnownTags.Method, CompletionItemKind.Method),
        (WellKnownTags.Property, CompletionItemKind.Property),
        (WellKnownTags.Event, CompletionItemKind.Event),
        (WellKnownTags.Constant, CompletionItemKind.Constant),
        (WellKnownTags.EnumMember, CompletionItemKind.EnumMember),
        (WellKnownTags.Field, CompletionItemKind.Field),
        (WellKnownTags.Local, CompletionItemKind.Variable),
        (WellKnownTags.RangeVariable, CompletionItemKind.Variable),
        (WellKnownTags.Parameter, CompletionItemKind.Parameter),
        (WellKnownTags.Class, CompletionItemKind.Class),
        (WellKnownTags.Structure, CompletionItemKind.Struct),
        (WellKnownTags.Interface, CompletionItemKind.Interface),
        (WellKnownTags.Enum, CompletionItemKind.Enum),
        (WellKnownTags.Delegate, CompletionItemKind.Delegate),
        (WellKnownTags.TypeParameter, CompletionItemKind.TypeParameter),
        (WellKnownTags.Namespace, CompletionItemKind.Namespace),
        (WellKnownTags.Keyword, CompletionItemKind.Keyword),
        (WellKnownTags.Snippet, CompletionItemKind.Snippet),
    ];

    public static CompletionItemKind FromTags(ImmutableArray<string> tags)
    {
        if (tags.IsDefaultOrEmpty)
            return CompletionItemKind.Text;

        foreach (var (tag, kind) in Kinds)
        {
            if (tags.Contains(tag))
                return kind;
        }

        return CompletionItemKind.Text;
    }
}
