namespace Quill.Data.Sql;

/// <summary>Database of a server.</summary>
/// <param name="Name">Name of the database.</param>
/// <param name="IsSystem">System database (master, model, msdb, tempdb).</param>
public sealed record SqlDatabaseInfo(string Name, bool IsSystem = false);
