using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Quill.Code.Analysis;
using Quill.Code.CSharp;
using Quill.Code.CSharp.Compilation;

namespace Quill.Code.Tests.CSharp;

public sealed class CSharpCompilerTests
{
    [Fact]
    public async Task Compile_LoadsTheAssembly()
    {
        var service = new CSharpLanguageService(() => CSharpTestProjects.LibraryOptions("Compiled"));
        using var compiled = await service.CompileAsync("public static class Calc { public static int Twice(int x) => x * 2; }");

        Assert.True(compiled.Success, string.Join("\n", compiled.Errors));
        var twice = compiled.Assembly!.GetType("Calc")!.GetMethod("Twice")!;
        Assert.Equal(42, twice.Invoke(null, [21]));
    }

    [Fact]
    public async Task Compile_ReferenceCompilesAnotherProject()
    {
        var library = new CSharpLanguageService(() => CSharpTestProjects.LibraryOptions("Library"));
        var model = await library.CompileAsync("public static class Model { public static int Answer => 42; }", new CSharpCompileOptions { Collectible = false });
        model.Dispose(); // no effect: assembly not collectible

        var service = new CSharpLanguageService(() => CSharpTestProjects.LibraryOptions("Client") with
        {
            References = [.. CSharpTestProjects.References, model.Reference!],
        });
        using var compiled = await service.CompileAsync("public static class Q { public static int Run() => Model.Answer; }", new CSharpCompileOptions
        {
            ResolveAssembly = name => name.Name == model.Assembly!.GetName().Name ? model.Assembly : null,
        });

        Assert.True(compiled.Success, string.Join("\n", compiled.Errors));
        Assert.Equal(42, compiled.Assembly!.GetType("Q")!.GetMethod("Run")!.Invoke(null, null));
    }

    [Fact]
    public async Task Compile_ErrorsPointToEditorText()
    {
        var service = new CSharpLanguageService(() => CSharpTestProjects.LibraryOptions("Errors"));

        const string code = "public static class Q { public static object Run() => 1 + unknown; }";
        using var compiled = await service.CompileAsync(code);

        Assert.False(compiled.Success);
        var error = Assert.Single(compiled.Errors);
        Assert.Equal("unknown", code.Substring(error.Start, error.Length));
        Assert.StartsWith($"Line 1, column {code.IndexOf("unknown", StringComparison.Ordinal) + 1}: CS0103 ", error.Describe(code), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Compile_ErrorWithoutPositionIsStillReported()
    {
        // No entry point (CS5001): no position, but never silent.
        var service = new CSharpLanguageService(() => CSharpTestProjects.LibraryOptions("NoEntryPoint") with
        {
            CompilationOptions = new CSharpCompilationOptions(OutputKind.ConsoleApplication),
        });

        using var compiled = await service.CompileAsync("public static class Q { }");

        var error = Assert.Single(compiled.Errors);
        Assert.Equal((0, 0), (error.Start, error.Length));
    }
}
