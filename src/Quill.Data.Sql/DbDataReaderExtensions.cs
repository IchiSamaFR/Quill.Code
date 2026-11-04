using System.Data.Common;
using System.Globalization;

namespace Quill.Data.Sql;

/// <summary>
/// Lecture tolerante des colonnes du catalogue, commune aux lecteurs de moteur : chaque pilote rend booleens et nombres
/// sous son propre type (bit, tinyint, bigint, NUMBER...), NULL compris.
/// </summary>
public static class DbDataReaderExtensions
{
    /// <summary>Booleen, quel que soit son type SQL (bit, entier, NUMBER) ; NULL = faux.</summary>
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

    /// <summary>Entier, quel que soit son type SQL ; NULL = 0.</summary>
    public static int GetInt32Value(this DbDataReader reader, int ordinal)
    {
        ArgumentNullException.ThrowIfNull(reader);

        var value = reader.GetValue(ordinal);
        return value is DBNull ? 0 : Convert.ToInt32(value, CultureInfo.InvariantCulture);
    }

    /// <summary>Precision ou echelle, bornee a un octet ; NULL = 0.</summary>
    public static byte GetByteValue(this DbDataReader reader, int ordinal)
        => (byte)Math.Clamp(reader.GetInt32Value(ordinal), byte.MinValue, byte.MaxValue);

    /// <summary>Texte non vide sans blancs de bord ; null si NULL ou vide (ex. commentaire absent).</summary>
    public static string? GetTrimmedStringOrNull(this DbDataReader reader, int ordinal)
    {
        ArgumentNullException.ThrowIfNull(reader);

        if (reader.IsDBNull(ordinal))
            return null;

        var text = Convert.ToString(reader.GetValue(ordinal), CultureInfo.InvariantCulture);
        return string.IsNullOrWhiteSpace(text) ? null : text.Trim();
    }
}
