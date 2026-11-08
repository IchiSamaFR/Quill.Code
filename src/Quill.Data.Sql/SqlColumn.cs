namespace Quill.Data.Sql;

/// <summary>Column of a table or view.</summary>
/// <param name="Name">Name of the column.</param>
/// <param name="DataType">Type as displayed, length or precision included (e.g. <c>nvarchar(50)</c>).</param>
public sealed record SqlColumn(string Name, string DataType)
{
    /// <summary>Base system type, aliases resolved (e.g. <c>nvarchar</c>); empty if unknown.</summary>
    public string SqlType { get; init; } = string.Empty;

    /// <summary>Accepts NULL.</summary>
    public bool IsNullable { get; init; } = true;

    /// <summary>Position in the primary key (1-based); 0 outside the key.</summary>
    public int PrimaryKeyOrdinal { get; init; }

    /// <summary>Precision of a numeric type; 0 if not applicable.</summary>
    public byte Precision { get; init; }

    /// <summary>Scale of a numeric or temporal type; 0 if not applicable.</summary>
    public byte Scale { get; init; }

    /// <summary>Part of a foreign key.</summary>
    public bool IsForeignKey { get; init; }

    /// <summary>IDENTITY column.</summary>
    public bool IsIdentity { get; init; }

    /// <summary>Computed column.</summary>
    public bool IsComputed { get; init; }

    /// <summary>Comment (MS_Description on SQL Server); null if missing.</summary>
    public string? Comment { get; init; }

    /// <summary>Part of the primary key.</summary>
    public bool IsPrimaryKey => PrimaryKeyOrdinal > 0;

    /// <summary>SSMS-style summary: "PK, FK, int, not null".</summary>
    public string Description
    {
        get
        {
            var parts = new List<string>(5);
            if (IsPrimaryKey)
                parts.Add("PK");
            if (IsForeignKey)
                parts.Add("FK");
            parts.Add(IsComputed ? $"calculée, {DataType}" : DataType);
            if (IsIdentity)
                parts.Add("identity");
            parts.Add(IsNullable ? "null" : "not null");
            return string.Join(", ", parts);
        }
    }
}
