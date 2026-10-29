using Quill.Code.CSharp;

namespace Quill.Code.Tests.CSharp;

public sealed class CSharpSourcesTests
{
    private const string Helper = "public static class FileHelper { public static int GetKbInBytes(int kb) => kb * 1024; }";
    private const string Caller = "public static class Calc { public static int Run() => FileHelper.GetKbInBytes(2); }";

    private static CSharpLanguageService CreateService() => new(() => CSharpTestProjects.LibraryOptions("Sources"));

    [Fact]
    public async Task Compile_UsesSourceAddedOnTheFly()
    {
        var service = CreateService();
        service.SetSource("FileHelper.cs", Helper);

        using var compiled = await service.CompileAsync(Caller);

        Assert.True(compiled.Success, string.Join("\n", compiled.Errors));
        Assert.Equal(2048, compiled.Assembly!.GetType("Calc")!.GetMethod("Run")!.Invoke(null, null));
    }

    [Fact]
    public async Task RemoveSource_RemovesItFromTheProject()
    {
        var service = CreateService();
        service.SetSource("FileHelper.cs", Helper);
        Assert.True(service.RemoveSource("FileHelper.cs"));

        using var compiled = await service.CompileAsync(Caller);

        Assert.Contains(compiled.Errors, e => e.Code == "CS0103");
        Assert.Empty(service.Sources);
    }

    [Fact]
    public async Task SetSource_ReplacesTheExistingSource()
    {
        var service = CreateService();
        service.SetSource("FileHelper.cs", Helper);
        service.SetSource("FileHelper.cs", Helper.Replace("1024", "1000", StringComparison.Ordinal));

        using var compiled = await service.CompileAsync(Caller);

        Assert.Equal(2000, compiled.Assembly!.GetType("Calc")!.GetMethod("Run")!.Invoke(null, null));
        Assert.Single(service.Sources);
    }

    [Fact]
    public async Task Compile_ErrorInSourceNamesTheFile()
    {
        var service = CreateService();
        service.SetSource("Broken.cs", "public static class Broken { public static int X() => nope; }");

        using var compiled = await service.CompileAsync("public static class Ok { }");

        var error = Assert.Single(compiled.Errors);
        Assert.Equal((0, 0), (error.Start, error.Length));
        Assert.StartsWith("Broken.cs (1, ", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Completion_SeesSourceAddedOnTheFly()
    {
        var service = CreateService();
        service.SetSource("FileHelper.cs", Helper);

        const string code = "class C { int M() => FileHelper. }";
        var list = await service.GetCompletionsAsync(code, code.IndexOf('.', StringComparison.Ordinal) + 1);

        Assert.Contains(list.Items, i => i.Label == "GetKbInBytes");
    }

    [Fact]
    public async Task SetSourceFile_ReadsTheFileAndRaisesSourcesChanged()
    {
        var path = Path.Combine(Path.GetTempPath(), $"FileHelper_{Guid.NewGuid():N}.cs");
        await File.WriteAllTextAsync(path, Helper);
        try
        {
            var service = CreateService();
            var changes = 0;
            service.SourcesChanged += (sender, _) =>
            {
                Assert.Same(service, sender);
                changes++;
            };

            var key = await service.SetSourceFileAsync(path);
            using var compiled = await service.CompileAsync(Caller);

            Assert.True(compiled.Success, string.Join("\n", compiled.Errors));
            Assert.Equal(Path.GetFullPath(path), key);
            Assert.Equal(1, changes);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
