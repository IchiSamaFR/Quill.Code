namespace Quill.Code.Completion;

/// <summary>Origine de l'ouverture de l'autocompletion.</summary>
public enum CompletionTrigger
{
    /// <summary>Frappe d'un caractère d'identifiant.</summary>
    Typing,

    /// <summary>Demande explicite (Ctrl+Espace).</summary>
    Explicit,

    /// <summary>Frappe d'un point (objet.membre, schema.table).</summary>
    MemberAccess,
}
