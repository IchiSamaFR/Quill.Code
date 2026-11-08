using System.Data.Common;
using System.Globalization;

namespace Quill.Data.Sql;

/// <summary>
/// Tolerant reading of catalog columns, shared by the engine readers: each driver returns booleans and numbers
/// under its own type (bit, tinyint, bigint, NUMBER...), NULL included.
/// </summary>
public static class DbDataReaderExtensions
{
    /// <summary>Boolean, whatever its SQL type (bit, integer, NUMBER); NULL = false.</summary>
    public static bool GetFlag(this DbDataReader reader, int ordinal)
    {
        ArgumentNullException.ThrowIfNull(reader);

        return reader.GetValue(ordinal) switch
        {
            bool b => b,
            DBNull => false,
            var value => Convert.ToInt64(value, CultureInfo.InvariantCulture) != 0,
        };
    }

    /// <summary>Integer, whatever its SQL type; NULL = 0.</summary>
    public static int GetInt32Value(this DbDataReader reader, int ordinal)
    {
        ArgumentNullException.ThrowIfNull(reader);

        var value = reader.GetValue(ordinal);
        return value is DBNull ? 0 : Convert.ToInt32(value, CultureInfo.InvariantCulture);
    }

    /// <summary>Precision or scale, bounded to one byte; NULL = 0.</summary>
    public static byte GetByteValue(this DbDataReader reader, int ordinal)
        => (byte)Math.Clamp(reader.GetInt32Value(ordinal), byte.MinValue, byte.MaxValue);

    /// <summary>Non-empty text without surrounding whitespace; null if NULL or empty (e.g. missing comment).</summary>
    public static string? GetTrimmedStringOrNull(this DbDataReader reader, int ordinal)
    {
        ArgumentNullException.ThrowIfNull(reader);

        if (reader.IsDBNull(ordinal))
            return null;

        var text = Convert.ToString(reader.GetValue(ordinal), CultureInfo.InvariantCulture);
        return string.IsNullOrWhiteSpace(text) ? null : text.Trim();
    }
}
