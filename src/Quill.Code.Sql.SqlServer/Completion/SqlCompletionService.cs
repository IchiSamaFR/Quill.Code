using System.Text.RegularExpressions;
using Quill.Code.Completion;
using Quill.Code.Sql.Dialects;
using Quill.Code.Sql.SqlServer.Lexing;
using Quill.Code.Sql.SqlServer.Schema;

namespace Quill.Code.Sql.SqlServer.Completion;

/// <summary>
/// SQL completion: dialect words (<see cref="SqlLanguageOptions.Dialect"/>) and catalog objects depending on what the grammar
/// expects at the caret (<see cref="SqlContextAnalyzer"/>), words already written in the document, variables declared in the
/// script and host parameters.
/// </summary>
internal sealed partial class SqlCompletionService : ICodeCompletion
{
    /// <summary>Only words expected after a table name: the list must get in the way of typing an alias as little as possible.</summary>
    private static readonly HashSet<string> FollowTableName = new(StringComparer.OrdinalIgnoreCase)
    {
        "AS", "WITH", "WHERE", "JOIN", "INNER", "LEFT", "RIGHT", "FULL", "OUTER", "CROSS", "APPLY", "ON",
        "GROUP", "ORDER", "HAVING", "UNION", "EXCEPT", "INTERSECT", "SET", "VALUES", "SELECT", "OUTPUT", "OFFSET",
    };

    private readonly SqlLanguageOptions _options;
    private readonly SqlDialect _dialect;
    private readonly SqlSchemaCompletion _schema;
    private readonly CompletionItem[] _languageItems;
    private readonly HashSet<string> _followTableName;

    public SqlCompletionService(SqlLanguageOptions options)
    {
        _options = options;
        _dialect = options.Dialect;
        _schema = new SqlSchemaCompletion(new SqlCatalogReader(options.Catalog, _dialect), _dialect);
        _followTableName = new HashSet<string>([.. FollowTableName, .. _dialect.ClauseKeywords], StringComparer.OrdinalIgnoreCase);

        // The same word can be a type and a function (MAX, YEAR): it keeps both kinds.
        _languageItems =
        [
            .. _dialect.Keywords.Select(w => new CompletionItem { Label = w, Kind = CompletionItemKind.Keyword }),
            .. _dialect.Phrases.Select(w => new CompletionItem { Label = w, Kind = CompletionItemKind.Keyword }),
            .. _dialect.DataTypes.Select(w => new CompletionItem { Label = w, Kind = CompletionItemKind.DataType }),
            .. _dialect.Functions.Select(w => new CompletionItem { Label = w, Kind = CompletionItemKind.Function }),
        ];
    }

    /// <inheritdoc />
    public async Task<CompletionList> GetCompletionsAsync(string code, int position, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(code);

        position = Math.Clamp(position, 0, code.Length);
        var wordStart = SqlContextAnalyzer.FindWordStart(code, position);
        if (SqlContextAnalyzer.IsVariableWord(code, wordStart, position, _dialect))
            return new CompletionList(VariableItems(code, wordStart, position), wordStart, position - wordStart);

        var context = SqlContextAnalyzer.Analyze(code, position, _options.Database, _dialect);
        if (context.Kind == SqlContextKind.None)
            return CompletionList.Empty;

        var schemaItems = await _schema.GetItemsAsync(context, cancellationToken).ConfigureAwait(false);
        var items = Merge([KeywordItems(context), schemaItems, DocumentWords(code, wordStart, context)]);

        return new CompletionList(items, wordStart, position - wordStart);
    }

    /// <summary>The contract does not give the caret: the replaced word is the one ending <paramref name="code"/>.</summary>
    public Task<CompletionChange> GetChangeAsync(string code, CompletionItem item, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(code);
        ArgumentNullException.ThrowIfNull(item);

        var start = SqlContextAnalyzer.FindWordStart(code, code.Length);
        return Task.FromResult(new CompletionChange(start, code.Length - start, item.TextToInsert));
    }

    /// <summary>Table, database, member or procedure: slot reserved for schema objects.</summary>
    private IReadOnlyList<CompletionItem> KeywordItems(SqlContext context)
    {
        Func<CompletionItem, bool>? accepts = context.Kind switch
        {
            SqlContextKind.General => static _ => true,
            SqlContextKind.Column => static item => item.Kind != CompletionItemKind.DataType,
            SqlContextKind.DataType => static item => item.Kind == CompletionItemKind.DataType,
            SqlContextKind.AfterTableName => item => _followTableName.Contains(FirstWord(item.Label)),
            _ => null,
        };

        return accepts is null ? [] : _languageItems.Where(accepts).ToArray();
    }

    /// <summary>A phrase (FETCH FIRST 10 ROWS ONLY) follows the rules of its first word.</summary>
    private static string FirstWord(string label)
    {
        var space = label.IndexOf(' ', StringComparison.Ordinal);
        return space < 0 ? label : label[..space];
    }

    /// <summary>
    /// Identifiers already written (tables, columns, aliases), like VS Code word suggestions: useful without a
    /// catalog. The word being typed is excluded: it would suggest itself.
    /// </summary>
    private IReadOnlyList<CompletionItem> DocumentWords(string code, int wordStart, SqlContext context)
    {
        if (context.Kind is SqlContextKind.DataType or SqlContextKind.AfterTableName)
            return [];

        return SqlLexer.Tokenize(code, _dialect)
            .Where(token => token.Kind is SqlTokenKind.Word or SqlTokenKind.QuotedIdentifier && token.Start != wordStart)
            .Select(token => token.GetText(code))
            .Where(word => word.Length > 1 && !_dialect.IsLanguageWord(word))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(word => new CompletionItem { Label = word, Kind = CompletionItemKind.Text })
            .ToArray();
    }

    /// <summary>
    /// Variables already written in the script (DECLARE…) and host parameters, suggested when the current word is a
    /// variable: <c>@name</c>, or <c>:name</c> in Oracle.
    /// </summary>
    private CompletionItem[] VariableItems(string code, int wordStart, int position)
    {
        // The @ is part of the current word; Oracle's : precedes it.
        var typedStart = wordStart < position && code[wordStart] == '@' ? wordStart : wordStart - 1;
        var prefix = code[typedStart];

        var tokens = SqlLexer.Tokenize(code, _dialect);
        if (tokens.Any(t => t.Kind is SqlTokenKind.String or SqlTokenKind.QuotedIdentifier or SqlTokenKind.LineComment or SqlTokenKind.BlockComment
                && t.Start < position && position <= t.End))
            return [];

        var types = DeclaredTypes().Matches(code)
            .GroupBy(m => m.Groups[1].Value, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().Groups[2].Value, StringComparer.OrdinalIgnoreCase);

        // A parameter written with the other prefix is not completed: it would be inserted next to the typed prefix.
        var host = _options.Variables
            .Where(_ => prefix == _dialect.ParameterPrefix)
            .Select(v => VariableItem(_dialect.FormatParameter(v.Name), v.Type));

        var declared = tokens
            .Where(t => t.Kind == SqlTokenKind.Variable && t.Start != typedStart)
            .Select(t => t.GetText(code))
            .Where(name => name[0] == prefix && !name.StartsWith("@@", StringComparison.Ordinal))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(name => VariableItem(name, types.GetValueOrDefault(name)));

        // Host parameters (dashboard) come first: a DECLARE with the same name does not duplicate them.
        return host.Concat(declared)
            .GroupBy(i => i.Label, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .ToArray();
    }

    /// <summary>The <c>:</c> is not part of the replaced word: only the name is inserted, and used for filtering.</summary>
    private static CompletionItem VariableItem(string reference, string? type)
        => reference[0] == '@'
            ? new CompletionItem { Label = reference, Kind = CompletionItemKind.Variable, Detail = type }
            : new CompletionItem { Label = reference, Kind = CompletionItemKind.Variable, Detail = type, InsertText = reference[1..], FilterText = reference[1..] };

    [GeneratedRegex(@"\bDECLARE\s+(@\w+)\s+(?:AS\s+)?(\w+(?:\s*\([^)]*\))?)", RegexOptions.IgnoreCase)]
    private static partial Regex DeclaredTypes();

    private static CompletionItem[] Merge(IReadOnlyList<CompletionItem>[] batches)
    {
        // MAX is both a type and a function, Users may exist in two schemas: only true duplicates are removed.
        var seen = new HashSet<(string, CompletionItemKind, string?)>();
        var items = new List<CompletionItem>(batches.Sum(batch => batch.Count));
        foreach (var item in batches.SelectMany(batch => batch))
        {
            if (seen.Add((item.Label.ToUpperInvariant(), item.Kind, item.Detail)))
                items.Add(item);
        }

        // A document word already known from the schema or the keywords would be a duplicate, without type or icon.
        var known = items.Where(item => item.Kind != CompletionItemKind.Text)
            .Select(item => item.Label)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return items.Where(item => item.Kind != CompletionItemKind.Text || !known.Contains(SqlContextAnalyzer.Unquote(item.Label)))
            .ToArray();
    }
}
