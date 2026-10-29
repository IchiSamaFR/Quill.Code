namespace Quill.Code.Sql.Execution;

/// <summary>Parametre envoye avec une requete (null = NULL SQL), jamais concatene au texte.</summary>
public sealed record QueryParameter(string Name, QueryParameterType Type, object? Value);
