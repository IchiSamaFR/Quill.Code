using Microsoft.CodeAnalysis;
using Quill.Code.Analysis;

namespace Quill.Code.CSharp.Features;

/// <summary>Signature texts and hover details, like Visual Studio: types without namespace, C# keywords.</summary>
internal static class CSharpSymbolFormatter
{
    private static readonly SymbolDisplayFormat Format = SymbolDisplayFormat.MinimallyQualifiedFormat
        .AddMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);

    /// <summary>
    /// [Description] carried by the symbol, its type or the element of a collection (DbSet&lt;Student&gt;);
    /// for a method, the type declaring it.
    /// </summary>
    public static string? Details(ISymbol symbol)
    {
        if (symbol is IMethodSymbol method)
            return DeclaredBy(method);

        var type = symbol switch
        {
            ILocalSymbol local => local.Type,
            IParameterSymbol parameter => parameter.Type,
            IPropertySymbol property => property.Type,
            IFieldSymbol field => field.Type,
            _ => null,
        };

        return DescriptionOf(symbol)
            ?? (type is null ? null : DescriptionOf(type))
            ?? (type is INamedTypeSymbol { IsGenericType: true, TypeArguments: [var element, ..] } ? DescriptionOf(element) : null);
    }

    /// <summary>Overload of a method: prefix, parameters one by one, suffix.</summary>
    public static SignatureInfo ToSignature(IMethodSymbol method)
    {
        var typeArguments = method.IsGenericMethod
            ? $"<{string.Join(", ", method.TypeArguments.Select(t => t.ToDisplayString(Format)))}>"
            : string.Empty;

        var prefix = method.MethodKind == MethodKind.Constructor
            ? $"{method.ContainingType.ToDisplayString(Format)}("
            : $"{method.ReturnType.ToDisplayString(Format)} {method.Name}{typeArguments}(";

        return new SignatureInfo(
            prefix,
            method.Parameters.Select(p => p.ToDisplayString(Format)).ToArray(),
            ")",
            DeclaredBy(method));
    }

    private static string? DeclaredBy(IMethodSymbol method)
    {
        var container = (method.ReducedFrom ?? method).ContainingType;
        return container is null ? null : $"Déclarée par {container.ToDisplayString(Format)}";
    }

    // [Description] set by a code generator (e.g. Querify's DbContext) on types and their members.
    private static string? DescriptionOf(ISymbol symbol)
        => symbol.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass is { Name: "DescriptionAttribute" } && a.ConstructorArguments.Length == 1)
            ?.ConstructorArguments[0].Value as string;
}
