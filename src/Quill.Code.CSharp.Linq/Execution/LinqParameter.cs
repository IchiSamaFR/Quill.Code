using Quill.Code.Execution;

namespace Quill.Code.CSharp.Linq.Execution;

/// <summary>
/// Query parameter, exposed to the script by its name without @: a QueryGlobals property of this type, initialized to this value.
/// Captured by the query, EF sends it as a SQL parameter, never concatenated into the text.
/// </summary>
/// <param name="Type">C# type of the variable (int?, string, DateTime?...); <paramref name="Value"/> must be assignable to it.</param>
internal sealed record LinqParameter(string Name, Type Type, object? Value)
{
    /// <summary>
    /// Query parameter as a nullable C# variable (NULL allowed). An integer fits in an int when it can:
    /// compared with an int column, a long would force a CAST on the SQL side.
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
