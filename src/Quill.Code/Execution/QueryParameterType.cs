namespace Quill.Code.Execution;

/// <summary>Type of a query parameter, translated by each engine into its own type.</summary>
public enum QueryParameterType
{
    /// <summary>Text (nvarchar(max) on SQL Server).</summary>
    Text,

    /// <summary>64-bit integer.</summary>
    Integer,

    /// <summary>Decimal.</summary>
    Decimal,

    /// <summary>Date without time.</summary>
    Date,

    /// <summary>Date and time.</summary>
    DateTime,

    /// <summary>Boolean (bit).</summary>
    Boolean,

    /// <summary>Unique identifier.</summary>
    Guid,
}
