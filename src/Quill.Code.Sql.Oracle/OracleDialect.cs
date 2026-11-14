using Quill.Code.Sql.Constants;
using Quill.Code.Sql.Dialects;
using Quill.Code.Sql.Oracle.Constants;

namespace Quill.Code.Sql.Oracle;

/// <summary>Oracle dialect: <c>"NAME"</c> (upper case when unquoted), <c>:param</c>, <c>FETCH FIRST n ROWS ONLY</c>, schema.object.</summary>
public static class OracleDialect
{
    /// <summary>Oracle dialect.</summary>
    public static SqlDialect Instance { get; } = new()
    {
        Name = "Oracle",
        IdentifierCasing = SqlIdentifierCasing.Upper,
        ExtraIdentifierChars = "$#",
        ParameterPrefix = ':',
        RowLimit = SqlRowLimitSyntax.FetchFirst,
        ProcedureKeywords = ["CALL", "EXEC", "EXECUTE"],
        Keywords = [.. SqlStandardKeywords.Keywords, .. OracleKeywords.Keywords],
        DataTypes = [.. SqlStandardKeywords.DataTypes, .. OracleKeywords.DataTypes],
        Functions = [.. SqlStandardKeywords.Functions, .. OracleKeywords.Functions],
        ClauseKeywords = OracleKeywords.ClauseKeywords,
        Phrases = OracleKeywords.Phrases,
    };
}
