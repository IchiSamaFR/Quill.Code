using System.IO;

namespace Quill.Code.Demo;

/// <summary>Texts embedded in the application (Resources folder): highlightings, samples, database script.</summary>
internal static class DemoResources
{
    public static string Read(string name)
    {
        using var stream = typeof(DemoResources).Assembly.GetManifestResourceStream(name)
            ?? throw new InvalidOperationException($"Resource {name} not found.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
