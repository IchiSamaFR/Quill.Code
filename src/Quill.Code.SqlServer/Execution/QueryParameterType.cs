namespace Quill.Code.SqlServer.Execution;

/// <summary>Type d'un parametre de requete, traduit par chaque moteur dans son propre type.</summary>
public enum QueryParameterType
{
    /// <summary>Texte (nvarchar(max) sur SQL Server).</summary>
    Text,

    /// <summary>Entier 64 bits.</summary>
    Integer,

    /// <summary>Decimal.</summary>
    Decimal,

    /// <summary>Date sans heure.</summary>
    Date,

    /// <summary>Date et heure.</summary>
    DateTime,

    /// <summary>Booleen (bit).</summary>
    Boolean,

    /// <summary>Identifiant unique.</summary>
    Guid,
}
