using Quill.Code.Sql.SqlServer.Schema;

namespace Quill.Code.Sql.SqlServer;

/// <summary>Context of a <see cref="SqlLanguageService"/>: what gives meaning to the names in the text.</summary>
public sealed record SqlLanguageOptions
{
    /// <summary>Catalog of databases; null: keywords, document words and variables only.</summary>
    public ISqlCatalog? Catalog { get; init; }

    /// <summary>Database queried by the text (before any USE); null = default database of the catalog.</summary>
    public string? Database { get; init; }

    /// <summary>Variables known before being declared (e.g. parameters provided at execution).</summary>
    public IReadOnlyList<SqlVariable> Variables { get; init; } = [];

    /// <summary>False: no T-SQL syntax errors (text in another dialect, e.g. MySQL or PostgreSQL); completion and hover remain.</summary>
    public bool CheckSyntax { get; init; } = true;
}
