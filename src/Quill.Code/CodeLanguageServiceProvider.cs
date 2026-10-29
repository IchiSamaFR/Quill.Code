namespace Quill.Code;

/// <summary>
/// Fournit les services de langage de code source connus par Quill.
/// </summary>
public static class CodeLanguageServiceProvider
{
    private static readonly List<ICodeLanguageProvider> _services = new List<ICodeLanguageProvider>();

    /// <summary>
    /// Enregistre un service de langage de code source.
    /// </summary>
    /// <param name="service">Service de langage de code source à enregistrer.</param>
    /// <exception cref="ArgumentNullException">Le service de langage de code source est null.</exception>
    /// <exception cref="InvalidOperationException">Un service pour le langage spécifié est déjà enregistré.</exception>
    public static void RegisterService(ICodeLanguageProvider service)
    {
        ArgumentNullException.ThrowIfNull(service);
        if (_services.Any(s => s.Language.Equals(service.Language, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException($"Le service pour le langage '{service.Language}' est déjà enregistré.");
        _services.Add(service);
    }

    /// <summary>
    /// Récupère le service de langage de code source correspondant au langage spécifié.
    /// </summary>
    /// <param name="language">Langage de code source.</param>
    /// <returns>Service de langage de code source correspondant.</returns>
    /// <exception cref="NotSupportedException">Le langage spécifié n'est pas pris en charge.</exception>
    public static ICodeLanguageProvider GetService(string language)
    {
        var service = _services.FirstOrDefault(s => s.Language.Equals(language, StringComparison.OrdinalIgnoreCase));
        if (service == null)
            throw new NotSupportedException($"Le langage '{language}' n'est pas pris en charge.");

        return service;
    }
}
