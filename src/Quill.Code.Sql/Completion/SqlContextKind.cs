namespace Quill.Code.Sql.Completion;

/// <summary>Ce que la grammaire attend à la position du curseur.</summary>
public enum SqlContextKind
{
    /// <summary>Commentaire, chaîne, variable, alias en cours de déclaration : rien à proposer.</summary>
    None,

    /// <summary>Début d'instruction ou position indéterminée.</summary>
    General,

    /// <summary>Expression : liste SELECT, WHERE, ON, SET, ORDER BY...</summary>
    Column,

    /// <summary>Après FROM, JOIN, UPDATE, INTO...</summary>
    TableSource,

    /// <summary>Juste après le nom d'une table : alias, ou suite de la requête (WHERE, JOIN...).</summary>
    AfterTableName,

    /// <summary>Après EXEC.</summary>
    Procedure,

    /// <summary>Après USE.</summary>
    Database,

    /// <summary>DECLARE @x |, CAST(x AS |).</summary>
    DataType,

    /// <summary>Après un qualificatif : alias.|, schema.|</summary>
    MemberAccess
}
