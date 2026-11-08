using Microsoft.SqlServer.TransactSql.ScriptDom;
using Quill.Code.Analysis;
using Quill.Code.Sql.SqlServer.Lexing;
using System.Globalization;
using Quill.Code.Sql.SqlServer.Constants;

namespace Quill.Code.Sql.SqlServer.Analysis;

/// <summary>
/// T-SQL syntax errors, from the SQL Server parser (ScriptDom): GO batches included, no connection.
/// Names (tables, columns, variables) are not checked: only the database could do that.
/// </summary>
public static class SqlSyntaxChecker
{
    /// <summary>Syntax errors of <paramref name="text"/>; empty list if the script is correct.</summary>
    public static IReadOnlyList<CodeDiagnostic> Check(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        if (string.IsNullOrWhiteSpace(text))
            return [];

        var parser = new TSql170Parser(initialQuotedIdentifiers: true);
        using var reader = new StringReader(text);
        parser.Parse(reader, out var errors);

        if (errors.Count == 0)
            return [];

        var tokens = SqlLexer.Tokenize(text);
        return errors
            .Take(SqlAnalysisConstants.MaxSyntaxDiagnostics)
            .Select(error => ToDiagnostic(text, tokens, error))
            .ToArray();
    }

    /// <summary>The parser only gives a position: the token found there is underlined.</summary>
    private static CodeDiagnostic ToDiagnostic(string text, IReadOnlyList<SqlToken> tokens, ParseError error)
    {
        var (start, length) = Locate(text, tokens, Math.Clamp(error.Offset, 0, text.Length));
        return new CodeDiagnostic(start, length, CodeDiagnosticSeverity.Error, error.Message, error.Number.ToString(CultureInfo.InvariantCulture));
    }

    private static (int Start, int Length) Locate(string text, IReadOnlyList<SqlToken> tokens, int offset)
    {
        foreach (var token in tokens)
        {
            if (token.Start <= offset && offset < token.End)
                return (token.Start, token.Length);
        }

        if (offset < text.Length && !char.IsWhiteSpace(text[offset]))
            return (offset, 1);

        // Unexpected end of text: the last visible character.
        var previous = Math.Min(offset, text.Length) - 1;
        while (previous > 0 && char.IsWhiteSpace(text[previous]))
            previous--;

        return (Math.Max(0, previous), text.Length == 0 ? 0 : 1);
    }
}
