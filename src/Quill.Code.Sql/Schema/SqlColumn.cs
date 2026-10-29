namespace Quill.Code.Sql.Schema;

/// <summary>Colonne d'une table ou d'une vue.</summary>
/// <param name="Name">Nom de la colonne.</param>
/// <param name="DataType">Type tel qu'affiché (ex. <c>nvarchar(50)</c>).</param>
/// <param name="IsNullable">Accepte NULL.</param>
/// <param name="IsPrimaryKey">Fait partie de la clé primaire.</param>
public sealed record SqlColumn(string Name, string DataType, bool IsNullable = true, bool IsPrimaryKey = false);
