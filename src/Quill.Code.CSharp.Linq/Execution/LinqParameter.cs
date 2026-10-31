using Quill.Code.Execution;

namespace Quill.Code.CSharp.Linq.Execution;

/// <summary>
/// Parametre de la requete, expose au script par son nom sans @ : propriete de QueryGlobals, de ce type, initialisee a cette valeur.
/// Capture par la requete, EF l'envoie comme parametre SQL, jamais concatene au texte.
/// </summary>
/// <param name="Type">Type C# de la variable (int?, string, DateTime?...) ; <paramref name="Value"/> doit lui etre affectable.</param>
internal sealed record LinqParameter(string Name, Type Type, object? Value)
{
    /// <summary>
    /// Parametre de requete en variable C# nullable (NULL autorise). Un entier tient en int quand il le peut :
    /// compare a une colonne int, un long forcerait un CAST cote SQL.
    /// </summary>
    public static LinqParameter From(QueryParameter parameter)
    {
        ArgumentNullException.ThrowIfNull(parameter);

        return parameter.Type switch
        {
            QueryParameterType.Integer when parameter.Value is long value and >= int.MinValue and <= int.MaxValue
                => new LinqParameter(parameter.Name, typeof(int?), (int)value),
            QueryParameterType.Integer when parameter.Value is long
                => new LinqParameter(parameter.Name, typeof(long?), parameter.Value),
            QueryParameterType.Integer => new LinqParameter(parameter.Name, typeof(int?), null),
            QueryParameterType.Decimal => new LinqParameter(parameter.Name, typeof(decimal?), parameter.Value),
            QueryParameterType.Date or QueryParameterType.DateTime => new LinqParameter(parameter.Name, typeof(DateTime?), parameter.Value),
            QueryParameterType.Boolean => new LinqParameter(parameter.Name, typeof(bool?), parameter.Value),
            QueryParameterType.Guid => new LinqParameter(parameter.Name, typeof(Guid?), parameter.Value),
            _ => new LinqParameter(parameter.Name, typeof(string), parameter.Value?.ToString()),
        };
    }
}
