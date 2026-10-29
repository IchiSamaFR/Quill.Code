namespace Quill.Code.Sql.Schema;

/// <summary>Nature d'un objet de base de données.</summary>
public enum SqlSchemaObjectKind
{
    /// <summary>Table.</summary>
    Table,

    /// <summary>Vue.</summary>
    View,

    /// <summary>Procédure stockée.</summary>
    Procedure,
}
