using System.Text.RegularExpressions;
using Quill.Code.Completion;
using Quill.Code.Sql.SqlServer.Lexing;
using Quill.Code.Sql.SqlServer.Schema;

namespace Quill.Code.Sql.SqlServer.Completion;

/// <summary>
/// Autocomplétion T-SQL : mots-clés et objets du catalogue selon ce qu'attend la grammaire au curseur
/// (<see cref="SqlContextAnalyzer"/>), mots déjà écrits dans le document et variables déclarées dans le script et paramètres de l'hôte.
/// </summary>
internal sealed partial class SqlCompletionService : ICodeCompletion
{
    /// <summary>Seuls mots attendus après un nom de table : la liste doit gêner le moins possible la frappe d'un alias.</summary>
    private static readonly HashSet<string> FollowTableName = new(StringComparer.OrdinalIgnoreCase)
    {
        "AS", "WITH", "WHERE", "JOIN", "INNER", "LEFT", "RIGHT", "FULL", "OUTER", "CROSS", "APPLY", "ON",
        "GROUP", "ORDER", "HAVING", "UNION", "EXCEPT", "INTERSECT", "SET", "VALUES", "SELECT", "OUTPUT", "OFFSET",
    };

    private readonly SqlLanguageOptions _options;
    private readonly SqlSchemaCompletion _schema;

    public SqlCompletionService(SqlLanguageOptions options)
    {
        _options = options;
        _schema = new SqlSchemaCompletion(new SqlCatalogReader(options.Catalog));
    }

    /// <inheritdoc />
    public async Task<CompletionList> GetCompletionsAsync(string code, int position, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(code);

        position = Math.Clamp(position, 0, code.Length);
        var wordStart = SqlContextAnalyzer.FindWordStart(code, position);
        if (wordStart < position && code[wordStart] == '@')
            return new CompletionList(VariableItems(code, wordStart, position), wordStart, position - wordStart);

        var context = SqlContextAnalyzer.Analyze(code, position, _options.Database);
        if (context.Kind == SqlContextKind.None)
            return CompletionList.Empty;

        var schemaItems = await _schema.GetItemsAsync(context, cancellationToken).ConfigureAwait(false);
        var items = Merge([KeywordItems(context), schemaItems, DocumentWords(code, wordStart, context)]);

        return new CompletionList(items, wordStart, position - wordStart);
    }

    /// <summary>Le contrat ne donne pas le curseur : le mot remplacé est celui qui termine <paramref name="code"/>.</summary>
    public Task<CompletionChange> GetChangeAsync(string code, CompletionItem item, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(code);
        ArgumentNullException.ThrowIfNull(item);

        var start = SqlContextAnalyzer.FindWordStart(code, code.Length);
        return Task.FromResult(new CompletionChange(start, code.Length - start, item.TextToInsert));
    }

    /// <summary>Table, base, membre ou procédure : place réservée aux objets du schéma.</summary>
    private static IReadOnlyList<CompletionItem> KeywordItems(SqlContext context)
    {
        Func<CompletionItem, bool>? accepts = context.Kind switch
        {
            SqlContextKind.General => static _ => true,
            SqlContextKind.Column => static item => item.Kind != CompletionItemKind.DataType,
            SqlContextKind.DataType => static item => item.Kind == CompletionItemKind.DataType,
            SqlContextKind.AfterTableName => static item => FollowTableName.Contains(item.Label),
            _ => null,
        };

        return accepts is null ? [] : SqlKeywords.Items.Where(accepts).ToArray();
    }

    /// <summary>
    /// Identifiants déjà écrits (tables, colonnes, alias), comme les suggestions par mot de VS Code : utiles sans
    /// catalogue. Le mot en cours de frappe est exclu : il se proposerait lui-même.
    /// </summary>
    private static IReadOnlyList<CompletionItem> DocumentWords(string code, int wordStart, SqlContext context)
    {
        if (context.Kind is SqlContextKind.DataType or SqlContextKind.AfterTableName)
            return [];

        return SqlLexer.Tokenize(code)
            .Where(token => token.Kind is SqlTokenKind.Word or SqlTokenKind.QuotedIdentifier && token.Start != wordStart)
            .Select(token => token.GetText(code))
            .Where(word => word.Length > 1 && !SqlKeywords.Words.Contains(word))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(word => new CompletionItem { Label = word, Kind = CompletionItemKind.Text })
            .ToArray();
    }

    /// <summary>Variables déjà écrites dans le script (DECLARE…), et paramètres de l'hôte, proposés quand le mot en cours commence par <c>@</c>.</summary>
    private CompletionItem[] VariableItems(string code, int wordStart, int position)
    {
        var tokens = SqlLexer.Tokenize(code);
        if (tokens.Any(t => t.Kind is SqlTokenKind.String or SqlTokenKind.QuotedIdentifier or SqlTokenKind.LineComment or SqlTokenKind.BlockComment
                && t.Start < position && position <= t.End))
            return [];

        var types = DeclaredTypes().Matches(code)
            .GroupBy(m => m.Groups[1].Value, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().Groups[2].Value, StringComparer.OrdinalIgnoreCase);

        var host = _options.Variables
            .Select(v => new CompletionItem { Label = v.Reference, Kind = CompletionItemKind.Variable, Detail = v.Type });

        var declared = tokens
            .Where(t => t.Kind == SqlTokenKind.Variable && t.Start != wordStart)
            .Select(t => t.GetText(code))
            .Where(name => !name.StartsWith("@@", StringComparison.Ordinal))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(name => new CompletionItem { Label = name, Kind = CompletionItemKind.Variable, Detail = types.GetValueOrDefault(name) });

        // Les paramètres de l'hôte (dashboard) passent en premier : un DECLARE du même nom ne les double pas.
        return host.Concat(declared)
            .GroupBy(i => i.Label, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .ToArray();
    }

    [GeneratedRegex(@"\bDECLARE\s+(@\w+)\s+(?:AS\s+)?(\w+(?:\s*\([^)]*\))?)", RegexOptions.IgnoreCase)]
    private static partial Regex DeclaredTypes();

    private static CompletionItem[] Merge(IReadOnlyList<CompletionItem>[] batches)
    {
        // MAX est à la fois type et fonction, Users peut exister dans deux schémas : seuls les vrais doublons partent.
        var seen = new HashSet<(string, CompletionItemKind, string?)>();
        var items = new List<CompletionItem>(batches.Sum(batch => batch.Count));
        foreach (var item in batches.SelectMany(batch => batch))
        {
            if (seen.Add((item.Label.ToUpperInvariant(), item.Kind, item.Detail)))
                items.Add(item);
        }

        // Un mot du document déjà connu du schéma ou des mots-clés ferait doublon, sans type ni icône.
        var known = items.Where(item => item.Kind != CompletionItemKind.Text)
            .Select(item => item.Label)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return items.Where(item => item.Kind != CompletionItemKind.Text || !known.Contains(item.Label.Trim('[', ']')))
            .ToArray();
    }
}
