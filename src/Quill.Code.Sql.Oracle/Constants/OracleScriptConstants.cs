using System.Collections.Frozen;

namespace Quill.Code.Sql.Oracle.Constants;

/// <summary>Mots qui annoncent un bloc PL/SQL, termine par une ligne « / » et non par « ; ».</summary>
internal static class OracleScriptConstants
{
    /// <summary>Premier mot d'un bloc anonyme.</summary>
    internal static readonly FrozenSet<string> AnonymousBlockKeywords = new[] { "DECLARE", "BEGIN" }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    /// <summary>Mots ignores entre CREATE et le type d'objet.</summary>
    internal static readonly FrozenSet<string> CreateModifiers = new[] { "OR", "REPLACE", "EDITIONABLE", "NONEDITIONABLE" }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    /// <summary>Objets dont le CREATE contient du PL/SQL.</summary>
    internal static readonly FrozenSet<string> BlockObjectTypes = new[] { "PROCEDURE", "FUNCTION", "PACKAGE", "TRIGGER", "TYPE", "LIBRARY" }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    internal const string CreateKeyword = "CREATE";

    /// <summary>Mots lus en tete d'instruction pour en reconnaitre la nature.</summary>
    internal const int MaxLeadingWords = 6;
}
