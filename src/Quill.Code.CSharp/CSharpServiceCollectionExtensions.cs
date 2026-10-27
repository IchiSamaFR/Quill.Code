using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Quill.Code.CSharp.Workspaces;

namespace Quill.Code.CSharp;

/// <summary>Enregistrement des services C# de Quill.</summary>
public static class CSharpServiceCollectionExtensions
{
    /// <summary>Workspace Roslyn partagé (<see cref="CSharpWorkspace"/>) et <see cref="ICSharpLanguageService"/>, en singletons.</summary>
    public static IServiceCollection AddQuillCSharp(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<CSharpWorkspace>();
        services.TryAddSingleton<ICSharpLanguageService, CSharpLanguageService>();
        return services;
    }
}
