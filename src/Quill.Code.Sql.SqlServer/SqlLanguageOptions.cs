using Quill.Code.Sql.SqlServer.Schema;

namespace Quill.Code.Sql.SqlServer;

/// <summary>Contexte d'un <see cref="SqlLanguageService"/> : ce qui donne un sens aux noms du texte.</summary>
public sealed record SqlLanguageOptions
{
    /// <summary>Catalogue des bases ; null : mots-clés, mots du document et variables seulement.</summary>
    public ISqlCatalog? Catalog { get; init; }

    /// <summary>Base interrogée par le texte (avant tout USE) ; null = base par défaut du catalogue.</summary>
    public string? Database { get; init; }

    /// <summary>Variables connues avant d'être déclarées (ex. paramètres fournis à l'exécution).</summary>
    public IReadOnlyList<SqlVariable> Variables { get; init; } = [];

    /// <summary>Faux : pas de fautes de syntaxe T-SQL (texte d'un autre dialecte, ex. MySQL ou PostgreSQL) ; l'autocomplétion et le survol restent.</summary>
    public bool CheckSyntax { get; init; } = true;
}
