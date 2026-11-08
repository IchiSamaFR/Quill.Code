using System.Collections.Frozen;

namespace Quill.Code.Sql.Oracle.Constants;

/// <summary>Words announcing a PL/SQL block, terminated by a "/" line rather than by ";".</summary>
internal static class OracleScriptConstants
{
    /// <summary>First word of an anonymous block.</summary>
    internal static readonly FrozenSet<string> AnonymousBlockKeywords = new[] { "DECLARE", "BEGIN" }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    /// <summary>Words skipped between CREATE and the object type.</summary>
    internal static readonly FrozenSet<string> CreateModifiers = new[] { "OR", "REPLACE", "EDITIONABLE", "NONEDITIONABLE" }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    /// <summary>Objects whose CREATE contains PL/SQL.</summary>
    internal static readonly FrozenSet<string> BlockObjectTypes = new[] { "PROCEDURE", "FUNCTION", "PACKAGE", "TRIGGER", "TYPE", "LIBRARY" }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    internal const string CreateKeyword = "CREATE";

    /// <summary>Words read at the start of a statement to recognize its kind.</summary>
    internal const int MaxLeadingWords = 6;
}
