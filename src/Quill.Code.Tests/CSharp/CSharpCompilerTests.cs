using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Quill.Code.Analysis;
using Quill.Code.CSharp;
using Quill.Code.CSharp.Compilation;
using Quill.Code.CSharp.Workspaces;

namespace Quill.Code.Tests.CSharp;

public sealed class CSharpCompilerTests
{
    private static CSharpLanguageService CreateService(Func<string, CSharpSource>? transform = null) => new("Compiled", () => new CSharpProjectOptions
    {
        References = CSharpReferences.FromTrustedPlatformAssemblies(),
        CompilationOptions = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary),
        SourceTransform = transform,
    });

    [Fact]
    public async Task Compile_LoadsTheAssembly()
    {
        using var service = CreateService();
        using var compiled = await service.CompileAsync("public static class Calc { public static int Twice(int x) => x * 2; }", new CSharpCompileOptions { EmitDebugInformation = true });

        Assert.True(compiled.Success, string.Join("\n", compiled.Errors));
        var twice = compiled.Assembly!.GetType("Calc")!.GetMethod("Twice")!;
        Assert.Equal(42, twice.Invoke(null, [21]));
    }

    [Fact]
    public async Task Compile_ReferenceCompilesAnotherProject()
    {
        using var library = new CSharpLanguageService("Library", () => new CSharpProjectOptions
        {
            References = CSharpReferences.FromTrustedPlatformAssemblies(),
            CompilationOptions = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary),
        });
        var model = await library.CompileAsync("public static class Model { public static int Answer => 42; }", new CSharpCompileOptions { Collectible = false });
        model.Dispose(); // sans effet : assembly non déchargeable

        using var service = new CSharpLanguageService("Client", () => new CSharpProjectOptions
        {
            References = [.. CSharpReferences.FromTrustedPlatformAssemblies(), model.Reference!],
            CompilationOptions = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary),
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
        const string prefix = "public static class Q { public static object Run() => ";
        using var service = CreateService(code => new CSharpSource(prefix + code + "; }", prefix.Length));

        const string code = "1 + unknown";
        using var compiled = await service.CompileAsync(code);

        Assert.False(compiled.Success);
        var error = Assert.Single(compiled.Errors);
        Assert.Equal("unknown", code.Substring(error.Start, error.Length));
        Assert.StartsWith("Ligne 1, colonne 5 : CS0103 ", error.Describe(code), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Compile_ErrorOutsideTextIsStillReported()
    {
        using var service = CreateService(code => new CSharpSource("class X { void M() { " + code + " } int Broken() { } }", "class X { void M() { ".Length));

        using var compiled = await service.CompileAsync("var a = 1;");

        var error = Assert.Single(compiled.Errors);
        Assert.Equal((0, 0), (error.Start, error.Length));
    }
}
