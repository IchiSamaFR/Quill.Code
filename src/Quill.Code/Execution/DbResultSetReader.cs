using System.Data.Common;
using System.Globalization;
using Quill.Code.Constants;

namespace Quill.Code.Execution;

/// <summary>Lecture d'un jeu de resultats ADO.NET en <see cref="QueryResultSet"/>, commune aux executeurs SQL.</summary>
public static class DbResultSetReader
{
    /// <summary>Lit le jeu courant de <paramref name="reader"/>, au plus <paramref name="maxRows"/> lignes.</summary>
    /// <param name="index">Position du jeu dans l'execution (base 1).</param>
    /// <param name="format">Mise en texte d'une valeur non nulle (<see cref="FormatForResults"/> ou <see cref="FormatForRead"/>).</param>
    public static async Task<QueryResultSet> ReadAsync(DbDataReader reader, int index, int maxRows, Func<object, string?> format, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(reader);
        ArgumentNullException.ThrowIfNull(format);

        var fieldCount = reader.FieldCount;
        var columns = new QueryResultColumn[fieldCount];
        for (var i = 0; i < fieldCount; i++)
        {
            var name = reader.GetName(i);
            columns[i] = new QueryResultColumn(string.IsNullOrEmpty(name) ? QueryResultConstants.UnnamedColumn : name, reader.GetDataTypeName(i));
        }

        var rows = new List<QueryResultRow>();
        var unsupportedColumns = new bool[fieldCount];
        var isTruncated = false;

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            if (rows.Count == maxRows)
            {
                isTruncated = true;
                break;
            }

            var cells = new string?[fieldCount];
            for (var i = 0; i < fieldCount; i++)
                cells[i] = ReadCell(reader, i, unsupportedColumns, format);

            rows.Add(new QueryResultRow(rows.Count + 1, cells));
        }

        return new QueryResultSet(index, columns, rows, isTruncated);
    }

    /// <summary>Grille de resultats : valeurs completes, comme SSMS.</summary>
    public static string? FormatForResults(object value) => value switch
    {
        string s => s,
        bool b => b ? "1" : "0",
        DateTime d => d.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture),
        DateTimeOffset d => d.ToString("yyyy-MM-dd HH:mm:ss.fffffff zzz", CultureInfo.InvariantCulture),
        DateOnly d => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        byte[] bytes => FormatBinary(bytes, QueryResultConstants.ResultsMaxBinaryBytes),
        IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
        _ => value.ToString(),
    };

    /// <summary>Requete parametree (cartes, graphiques) : dates courtes, binaires tronques plus tot.</summary>
    public static string? FormatForRead(object value) => value switch
    {
        DateTime d => d.TimeOfDay == TimeSpan.Zero
            ? d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
            : d.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
        DateTimeOffset d => d.ToString("yyyy-MM-dd HH:mm:ss zzz", CultureInfo.InvariantCulture),
        byte[] bytes => FormatBinary(bytes, QueryResultConstants.ReadMaxBinaryBytes),
        _ => FormatForResults(value),
    };

    private static string? ReadCell(DbDataReader reader, int ordinal, bool[] unsupportedColumns, Func<object, string?> format)
    {
        if (!unsupportedColumns[ordinal])
        {
            try
            {
                var value = reader.GetValue(ordinal);
                return value is DBNull ? null : format(value);
            }
            catch (Exception ex) when (ex is InvalidCastException or FileNotFoundException or NotSupportedException or OverflowException)
            {
                // Types sans equivalent .NET charge (geography SQL Server, NUMBER Oracle au-dela de decimal...).
                unsupportedColumns[ordinal] = true;
            }
        }

        return $"<{reader.GetDataTypeName(ordinal)}>";
    }

    private static string FormatBinary(byte[] bytes, int maxBytes)
    {
        var shown = Convert.ToHexString(bytes, 0, Math.Min(bytes.Length, maxBytes));
        return bytes.Length > maxBytes ? $"0x{shown}…" : $"0x{shown}";
    }
}
