namespace Quill.Code.Sql.SqlServer.Completion;

/// <summary>What the grammar expects at the caret position.</summary>
public enum SqlContextKind
{
    /// <summary>Comment, string, variable, alias being declared: nothing to suggest.</summary>
    None,

    /// <summary>Start of statement or undetermined position.</summary>
    General,

    /// <summary>Expression: SELECT list, WHERE, ON, SET, ORDER BY...</summary>
    Column,

    /// <summary>After FROM, JOIN, UPDATE, INTO...</summary>
    TableSource,

    /// <summary>Right after a table name: alias, or rest of the query (WHERE, JOIN...).</summary>
    AfterTableName,

    /// <summary>After EXEC.</summary>
    Procedure,

    /// <summary>After USE.</summary>
    Database,

    /// <summary>DECLARE @x |, CAST(x AS |).</summary>
    DataType,

    /// <summary>After a qualifier: alias.|, schema.|</summary>
    MemberAccess
}
