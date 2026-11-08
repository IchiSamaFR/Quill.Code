using Microsoft.EntityFrameworkCore;
using Quill.Code.CSharp.Linq.Constants;
using Quill.Code.Execution;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;

namespace Quill.Code.CSharp.Linq.Execution;

/// <summary>Values displayed by Dump() converted to result sets (grid): objects, scalars, dictionaries, EF queries.</summary>
[UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Compilation en memoire, sans trimming : reflexion sur les resultats assumee.")]
[UnconditionalSuppressMessage("Trimming", "IL2060", Justification = "Compilation en memoire, sans trimming : reflexion sur les resultats assumee.")]
[UnconditionalSuppressMessage("Trimming", "IL2070", Justification = "Compilation en memoire, sans trimming : reflexion sur les resultats assumee.")]
[UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "Compilation en memoire, sans trimming : reflexion sur les resultats assumee.")]
[UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Compilation en memoire, sans AOT : reflexion sur les resultats assumee.")]
internal static class LinqResultConverter
{
    private static readonly MethodInfo QueryableTake = typeof(Queryable).GetMethods()
        .Single(m => m.Name == nameof(Queryable.Take) && m.GetParameters()[1].ParameterType == typeof(int));

    /// <summary>
    /// Result set of a Dump(value, title), captured immediately: it shows the value at that moment, even if the
    /// script modifies it afterwards. An IQueryable query is limited on the server side; its SQL is returned separately.
    /// </summary>
    public static (QueryResultSet ResultSet, string? Sql) Capture(object? value, int index, string? title, int maxRows, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        string? sql = null;
        if (value is IQueryable queryable)
        {
            sql = TryGetSql(queryable);

            // Plafond applique cote serveur (TOP), une ligne de plus pour savoir s'il a ete atteint.
            value = QueryableTake.MakeGenericMethod(queryable.ElementType).Invoke(null, [queryable, maxRows + 1]);
        }

        return (ToResultSet(value, index, title, maxRows, cancellationToken), sql);
    }

    private static string? TryGetSql(IQueryable queryable)
    {
        try
        {
            return queryable.ToQueryString();
        }
        catch (Exception ex) when (ex is InvalidOperationException or NotSupportedException)
        {
            // Requete evaluee cote client (LINQ to Objects) : pas de SQL a montrer.
            return null;
        }
    }

    // ---------- Conversion en grille ----------

    private static QueryResultSet ToResultSet(object? value, int index, string? title, int maxRows, CancellationToken cancellationToken)
    {
        // Une valeur seule (Count(), First()...) s'affiche comme une ligne d'une colonne.
        if (value is null or string || value is not IEnumerable items || IsScalar(value.GetType()))
            return Build(ElementTypeOf(value?.GetType()), [value], isTruncated: false, index, title);

        List<object?> rows = [];
        var isTruncated = false;

        foreach (var item in items)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (rows.Count == maxRows)
            {
                isTruncated = true;
                break;
            }

            rows.Add(item);
        }

        var elementType = EnumerableElementType(value.GetType()) is { } declared && declared != typeof(object)
            ? declared
            : rows.FirstOrDefault(r => r is not null)?.GetType() ?? typeof(object);

        return Build(elementType, rows, isTruncated, index, title);
    }

    private static QueryResultSet Build(Type elementType, IReadOnlyList<object?> items, bool isTruncated, int index, string? title)
    {
        if (items.Any(i => i is not null) && items.All(i => i is null || AsPairs(i) is not null))
            return BuildFromDictionaries(items, isTruncated, index, title);

        if (IsScalar(elementType))
        {
            var column = new QueryResultColumn(LinqExecutionConstants.ScalarColumn, FriendlyName(elementType));
            var scalarRows = items.Select((item, i) => new QueryResultRow(i + 1, [Format(item)])).ToArray();
            return new QueryResultSet(index, [column], scalarRows, isTruncated, title);
        }

        var properties = elementType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.GetIndexParameters().Length == 0)
            .ToArray();

        var columns = properties.Select(p => new QueryResultColumn(p.Name, FriendlyName(p.PropertyType))).ToArray();
        var rows = items.Select((item, i) => new QueryResultRow(i + 1, properties.Select(p => item is null ? null : Format(p.GetValue(item))).ToArray())).ToArray();

        return new QueryResultSet(index, columns, rows, isTruncated, title);
    }

    // Colonnes dynamiques : union ordonnee des cles de toutes les lignes (une cle absente = cellule vide).
    private static QueryResultSet BuildFromDictionaries(IReadOnlyList<object?> items, bool isTruncated, int index, string? title)
    {
        var data = items.Select(i => (i is null ? Enumerable.Empty<KeyValuePair<string, object?>>() : AsPairs(i)!).ToList()).ToList();

        var names = data.SelectMany(d => d.Select(p => p.Key)).Distinct().ToList();
        var columns = names
            .Select(n => new QueryResultColumn(n, FriendlyName(data.SelectMany(d => d).FirstOrDefault(p => p.Key == n && p.Value is not null).Value?.GetType() ?? typeof(object))))
            .ToArray();

        var rows = data
            .Select((d, i) =>
            {
                var values = d.GroupBy(p => p.Key).ToDictionary(g => g.Key, g => g.Last().Value);
                return new QueryResultRow(i + 1, names.Select(n => values.TryGetValue(n, out var v) ? Format(v) : null).ToArray());
            })
            .ToArray();

        return new QueryResultSet(index, columns, rows, isTruncated, title);
    }

    private static IEnumerable<KeyValuePair<string, object?>>? AsPairs(object item) => item switch
    {
        IDictionary<string, object?> d => d,
        IDictionary d => d.Cast<DictionaryEntry>().Select(e => new KeyValuePair<string, object?>(Convert.ToString(e.Key, CultureInfo.InvariantCulture) ?? string.Empty, e.Value)),
        _ => null,
    };

    private static Type ElementTypeOf(Type? type) => type ?? typeof(object);

    private static Type? EnumerableElementType(Type type)
        => type.GetInterfaces().Append(type)
               .FirstOrDefault(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IEnumerable<>))
               ?.GetGenericArguments()[0];

    private static bool IsScalar(Type type)
    {
        var underlying = Nullable.GetUnderlyingType(type) ?? type;
        return underlying.IsPrimitive || underlying.IsEnum || underlying == typeof(object)
            || underlying == typeof(string) || underlying == typeof(decimal) || underlying == typeof(byte[])
            || underlying == typeof(DateTime) || underlying == typeof(DateTimeOffset) || underlying == typeof(DateOnly)
            || underlying == typeof(TimeOnly) || underlying == typeof(TimeSpan) || underlying == typeof(Guid);
    }

    /// <summary>Short C# name: int?, string, List&lt;Commande&gt;...</summary>
    private static string FriendlyName(Type type)
    {
        if (Nullable.GetUnderlyingType(type) is { } underlying)
            return FriendlyName(underlying) + "?";

        if (type.IsGenericType)
        {
            // Un type imbrique dans un generique (Dictionary<,>.KeyCollection) n'a pas de backtick.
            var tick = type.Name.IndexOf('`', StringComparison.Ordinal);
            var name = tick >= 0 ? type.Name[..tick] : type.Name;
            return $"{name}<{string.Join(", ", type.GetGenericArguments().Select(FriendlyName))}>";
        }

        return type.Name switch
        {
            "Int32" => "int",
            "Int64" => "long",
            "Int16" => "short",
            "Byte" => "byte",
            "Boolean" => "bool",
            "String" => "string",
            "Decimal" => "decimal",
            "Double" => "double",
            "Single" => "float",
            "Object" => "object",
            "Byte[]" => "byte[]",
            _ => type.Name
        };
    }

    /// <summary>Same display conventions as the SQL execution.</summary>
    private static string? Format(object? value) => value switch
    {
        null => null,
        string s => s,
        bool b => b ? "1" : "0",
        DateTime d => d.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture),
        DateTimeOffset d => d.ToString("yyyy-MM-dd HH:mm:ss.fffffff zzz", CultureInfo.InvariantCulture),
        DateOnly d => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        byte[] bytes => "0x" + Convert.ToHexString(bytes, 0, Math.Min(bytes.Length, LinqExecutionConstants.MaxBinaryBytes)) + (bytes.Length > LinqExecutionConstants.MaxBinaryBytes ? "…" : string.Empty),
        IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
        // Collection imbriquee (Select(u => u.Roles)...) : on en donne la taille plutot que le nom du type.
        IEnumerable e => $"({e.Cast<object?>().Count()} élément(s))",
        _ => value.ToString(),
    };
}
