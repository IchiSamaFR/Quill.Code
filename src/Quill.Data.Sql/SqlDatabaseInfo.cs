namespace Quill.Data.Sql;

/// <summary>Base d'un serveur.</summary>
/// <param name="Name">Nom de la base.</param>
/// <param name="IsSystem">Base système (master, model, msdb, tempdb).</param>
public sealed record SqlDatabaseInfo(string Name, bool IsSystem = false);
