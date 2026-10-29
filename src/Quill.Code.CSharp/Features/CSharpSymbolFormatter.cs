using Microsoft.CodeAnalysis;
using Quill.Code.Analysis;

namespace Quill.Code.CSharp.Features;

/// <summary>Textes des signatures et précisions du survol, comme Visual Studio : types sans espace de noms, mots-clés C#.</summary>
internal static class CSharpSymbolFormatter
{
    private static readonly SymbolDisplayFormat Format = SymbolDisplayFormat.MinimallyQualifiedFormat
        .AddMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);

    /// <summary>
    /// [Description] portée par le symbole, son type ou l'élément d'une collection (DbSet&lt;Student&gt;) ;
    /// pour une méthode, le type qui la déclare.
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

    /// <summary>Surcharge d'une méthode : préfixe, paramètres un par un, suffixe.</summary>
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

    // [Description] posé par un générateur de code (ex. DbContext de Querify) sur les types et leurs membres.
    private static string? DescriptionOf(ISymbol symbol)
        => symbol.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass is { Name: "DescriptionAttribute" } && a.ConstructorArguments.Length == 1)
            ?.ConstructorArguments[0].Value as string;
}
