namespace Quill.Code.Sql;

/// <summary>Résultat de l'analyse du texte autour du curseur.</summary>
public sealed class SqlContext
{
    /// <summary>Rien à proposer à cette position.</summary>
    public static SqlContext None { get; } = new() { Kind = SqlContextKind.None };

    /// <summary>Début d'instruction ou position indéterminée (langage sans analyse du contexte).</summary>
    public static SqlContext General { get; } = new() { Kind = SqlContextKind.General };

    /// <summary>Type de saisie attendu.</summary>
    public required SqlContextKind Kind { get; init; }

    /// <summary>Parties avant le point, dans l'ordre : ["u"] pour u.|, ["db", "dbo"] pour db.dbo.|</summary>
    public IReadOnlyList<string> Qualifier { get; init; } = [];

    /// <summary>Contexte du premier mot du qualificatif : FROM dbo.| (TableSource) n'attend pas la même chose que EXEC dbo.|</summary>
    public SqlContextKind MemberParentKind { get; init; } = SqlContextKind.General;

    /// <summary>Tables du lot courant, la plus proche du curseur en premier.</summary>
    public IReadOnlyList<SqlTableReference> Tables { get; init; } = [];

    /// <summary>Base du dernier USE avant le curseur, à défaut celle fournie à l'analyse ; null = base de la connexion.</summary>
    public string? Database { get; init; }

    /// <summary>Table désignée par un alias ou un nom simple (u.| ou Users.|).</summary>
    public SqlTableReference? FindTable(string exposedName)
        => Tables.FirstOrDefault(t => string.Equals(t.ExposedName, exposedName, StringComparison.OrdinalIgnoreCase));
}
