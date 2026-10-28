using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Quill.Code.CSharp;
using Quill.Code.CSharp.Compilation;
using Quill.Code.CSharp.Workspaces;
using Quill.Code.Scripting;

namespace Quill.Code.Tests.CSharp;

public sealed class CSharpScriptRuntimeTests
{
    private static CSharpLanguageService CreateService() => new("Script", () => new CSharpProjectOptions
    {
        References = CSharpReferences.FromTrustedPlatformAssemblies(),
        CompilationOptions = new CSharpCompilationOptions(OutputKind.ConsoleApplication, nullableContextOptions: NullableContextOptions.Enable),
        IncludeScriptRuntime = true,
    });

    [Fact]
    public async Task Log_IsBoundToTheCompileOptions()
    {
        using var service = CreateService();
        List<(LogLevel Level, string Text)> logs = [];
        using var compiled = await service.CompileAsync("""
            Log.Information("a");
            Log.Write(LogLevel.Warning, 42);
            Log.Write(LogLevel.None, "ignoré");
            return 3;
            """, new CSharpCompileOptions { Log = (level, text) => logs.Add((level, text)) });

        Assert.True(compiled.Success, string.Join("\n", compiled.Errors));
        Assert.Equal(3, await compiled.RunEntryPointAsync());
        Assert.Equal(new[] { (LogLevel.Information, "a"), (LogLevel.Warning, "42") }, logs);
    }

    [Fact]
    public async Task Log_IsSpecificToEachCompilation()
    {
        using var service = CreateService();
        List<string> first = [];
        List<string> second = [];
        using var a = await service.CompileAsync("Log.Information(\"a\");", new CSharpCompileOptions { Log = (_, text) => first.Add(text) });
        using var b = await service.CompileAsync("Log.Information(\"b\");", new CSharpCompileOptions { Log = (_, text) => second.Add(text) });

        await Task.WhenAll(a.RunEntryPointAsync(), b.RunEntryPointAsync());

        Assert.Equal(new[] { "a" }, first);
        Assert.Equal(new[] { "b" }, second);
    }

    [Fact]
    public async Task Log_WithoutRuntimeIsRejected()
    {
        using var service = new CSharpLanguageService("NoRuntime", () => new CSharpProjectOptions
        {
            References = CSharpReferences.FromTrustedPlatformAssemblies(),
            CompilationOptions = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary),
        });

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CompileAsync("public static class Q { }", new CSharpCompileOptions { Log = (_, _) => { } }));
    }

    [Fact]
    public async Task Run_ExposesTheCancellationToken()
    {
        using var service = CreateService();
        using var compiled = await service.CompileAsync("return Script.CancellationToken.CanBeCanceled ? 1 : 0;");
        using var cancellation = new CancellationTokenSource();

        Assert.Equal(1, await compiled.RunEntryPointAsync(cancellationToken: cancellation.Token));
    }

    [Fact]
    public async Task Run_RethrowsTheScriptException()
    {
        using var service = CreateService();
        using var compiled = await service.CompileAsync("throw new System.InvalidOperationException(\"boum\");");

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => compiled.RunEntryPointAsync());
        Assert.Equal("boum", exception.Message);
    }

    [Fact]
    public async Task Run_IsAbandonedWhenTheCodeIgnoresCancellation()
    {
        using var service = CreateService();
        using var compiled = await service.CompileAsync("System.Threading.Thread.Sleep(1000);");
        using var cancellation = new CancellationTokenSource(100);

        await Assert.ThrowsAsync<CSharpRunAbandonedException>(() => compiled.RunEntryPointAsync(cancellationToken: cancellation.Token));
    }
}
