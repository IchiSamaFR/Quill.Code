namespace Quill.Code.Sql.Schema;

/// <summary>Base du serveur.</summary>
/// <param name="Name">Nom de la base.</param>
/// <param name="IsSystem">Base système (master, tempdb…), signalée dans l'autocomplétion.</param>
public sealed record SqlDatabaseInfo(string Name, bool IsSystem = false);
