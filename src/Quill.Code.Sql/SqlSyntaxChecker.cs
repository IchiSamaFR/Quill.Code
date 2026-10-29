using Microsoft.SqlServer.TransactSql.ScriptDom;
using Quill.Code.Analysis;
using System.Globalization;

namespace Quill.Code.Sql;

/// <summary>
/// Fautes de syntaxe T-SQL, par le parseur de SQL Server (ScriptDom) : lots GO compris, sans connexion.
/// Les noms (tables, colonnes, variables) ne sont pas verifies : seule la base le pourrait.
/// </summary>
public static class SqlSyntaxChecker
{
    /// <summary>Apres une premiere faute, le parseur se resynchronise mal : les suivantes sont souvent des echos.</summary>
    private const int MaxDiagnostics = 20;

    /// <summary>Fautes de syntaxe de <paramref name="text"/> ; liste vide si le script est correct.</summary>
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
            .Take(MaxDiagnostics)
            .Select(error => ToDiagnostic(text, tokens, error))
            .ToArray();
    }

    /// <summary>Le parseur ne donne qu'une position : on souligne le jeton qui s'y trouve.</summary>
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

        // Fin de texte inattendue : le dernier caractere visible.
        var previous = Math.Min(offset, text.Length) - 1;
        while (previous > 0 && char.IsWhiteSpace(text[previous]))
            previous--;

        return (Math.Max(0, previous), text.Length == 0 ? 0 : 1);
    }
}
