namespace Quill.Code.Sql.SqlServer;

/// <summary>Variable provided by the host, usable without DECLARE (e.g. execution parameter).</summary>
/// <param name="Name">Name, with or without a prefix (<c>@</c>, <c>:</c>): it is written with the dialect's one.</param>
/// <param name="Type">Displayed SQL type (e.g. <c>int</c>).</param>
/// <param name="Description">Origin displayed on hover (e.g. "Dashboard parameter").</param>
public sealed record SqlVariable(string Name, string Type, string? Description = null);
