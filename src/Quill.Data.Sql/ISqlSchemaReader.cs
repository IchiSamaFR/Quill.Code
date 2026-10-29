namespace Quill.Data.Sql;

/// <summary>Lecture du catalogue d'un serveur ; une implémentation par moteur (ex. Quill.Data.Sql.SqlServer).</summary>
public interface ISqlSchemaReader
{
    /// <summary>Bases en ligne du serveur, triées par nom.</summary>
    Task<IReadOnlyList<SqlDatabaseInfo>> GetDatabasesAsync(string connectionString, CancellationToken cancellationToken = default);

    /// <summary>Objets utilisateur d'une nature donnée, sans leurs colonnes, triés par schéma puis nom.</summary>
    Task<IReadOnlyList<SqlSchemaObject>> GetObjectsAsync(string connectionString, string database, SqlSchemaObjectKind kind, CancellationToken cancellationToken = default);

    /// <summary>Colonnes d'une table ou d'une vue, dans l'ordre de déclaration.</summary>
    Task<IReadOnlyList<SqlColumn>> GetColumnsAsync(string connectionString, string database, string schema, string objectName, CancellationToken cancellationToken = default);

    /// <summary>Tables, vues, procédures et colonnes d'une base en un aller-retour.</summary>
    /// <param name="database">Nom de la base ; null = base de la chaîne de connexion.</param>
    Task<SqlDatabaseSchema> GetSchemaAsync(string connectionString, string? database, CancellationToken cancellationToken = default);
}
