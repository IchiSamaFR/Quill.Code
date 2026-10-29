namespace Quill.Data.Sql;

/// <summary>Colonne d'une table ou d'une vue.</summary>
/// <param name="Name">Nom de la colonne.</param>
/// <param name="DataType">Type tel qu'affiché, longueur ou précision comprise (ex. <c>nvarchar(50)</c>).</param>
public sealed record SqlColumn(string Name, string DataType)
{
    /// <summary>Type système de base, alias résolus (ex. <c>nvarchar</c>) ; vide s'il n'est pas connu.</summary>
    public string SqlType { get; init; } = string.Empty;

    /// <summary>Accepte NULL.</summary>
    public bool IsNullable { get; init; } = true;

    /// <summary>Position dans la clé primaire (base 1) ; 0 hors clé.</summary>
    public int PrimaryKeyOrdinal { get; init; }

    /// <summary>Précision d'un type numérique ; 0 si sans objet.</summary>
    public byte Precision { get; init; }

    /// <summary>Échelle d'un type numérique ou temporel ; 0 si sans objet.</summary>
    public byte Scale { get; init; }

    /// <summary>Fait partie d'une clé étrangère.</summary>
    public bool IsForeignKey { get; init; }

    /// <summary>Colonne IDENTITY.</summary>
    public bool IsIdentity { get; init; }

    /// <summary>Colonne calculée.</summary>
    public bool IsComputed { get; init; }

    /// <summary>Commentaire (MS_Description sous SQL Server) ; null si absent.</summary>
    public string? Comment { get; init; }

    /// <summary>Fait partie de la clé primaire.</summary>
    public bool IsPrimaryKey => PrimaryKeyOrdinal > 0;

    /// <summary>Résumé façon SSMS : « PK, FK, int, not null ».</summary>
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
