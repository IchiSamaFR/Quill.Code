namespace Quill.Code.Sql.SqlServer;

/// <summary>Variable provided by the host, usable without DECLARE (e.g. execution parameter).</summary>
/// <param name="Name">Name, with or without <c>@</c>.</param>
/// <param name="Type">Displayed SQL type (e.g. <c>int</c>).</param>
/// <param name="Description">Origin shown on hover (e.g. "Dashboard parameter").</param>
public sealed record SqlVariable(string Name, string Type, string? Description = null)
{
    /// <summary>Name as written in the text, with <c>@</c>.</summary>
    public string Reference => "@" + Name.TrimStart('@');
}
