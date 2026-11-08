using Quill.Code.CSharp.Linq.Compilation;
using Quill.Code.CSharp.Linq.SqlServer;
using Quill.Code.CSharp.Linq.Execution;
using Quill.Code.Analysis;
using Quill.Code.Completion;
using Quill.Data.Sql;
using System.Collections.Concurrent;

namespace Quill.Code.Tests.Linq;

/// <summary>
/// Analysis of a query in its generated context, as the LINQ editor does once the DbContext is resolved
/// (services of <see cref="LinqCompiler"/>, shared: MEF composition is expensive).
/// </summary>
internal static class TestLinq
{
    /// <summary>Test engine, a single instance: compiled DbContexts are cached per provider.</summary>
    public static SqlServerLinqProvider Provider { get; } = new("Quill.Code.Tests");

    /// <summary>Test structure, a single instance: its compiled model is shared by all classes (LinqContexts cache).</summary>
    public static SqlDatabaseSchema Schema { get; } = EfModelGeneratorTests.SampleSnapshot();

    /// <summary>Compiled DbContext of <see cref="Schema"/>.</summary>
    public static Lazy<LinqGeneratedModelAssembly> Model { get; } = new(() => LinqContexts.GetModelAsync(Provider, Schema).GetAwaiter().GetResult());

    /// <summary>Native C# query where <paramref name="expression"/> is the value of a variable; the context is named Db.</summary>
    public static string Expression(string expression) => $"var result = {expression};\n";

    /// <summary>Native C# query made of the <paramref name="statements"/> statements.</summary>
    public static string Statements(string statements) => statements + "\n";

    /// <summary>QueryGlobals compiled once per model and set of parameters (names and types), as in LinqContexts.</summary>
    private static readonly ConcurrentDictionary<(LinqGeneratedModelAssembly?, string), Lazy<LinqParametersAssembly>> GlobalsCache = new();

    /// <summary>QueryGlobals of <paramref name="model"/> (null = C# only) and of the <paramref name="parameters"/>.</summary>
    public static LinqParametersAssembly Globals(LinqGeneratedModelAssembly? model, IReadOnlyList<LinqParameter>? parameters = null)
    {
        parameters ??= [];
        return GlobalsCache.GetOrAdd((model, LinqQuerySource.BuildGlobals(model?.Model, parameters)),
            _ => new(() => LinqCompiler.CompileParametersAsync(model, parameters).GetAwaiter().GetResult())).Value;
    }

    public static async Task<IReadOnlyList<CompletionItem>> CompleteAsync(LinqGeneratedModelAssembly? model, string query, int caret, IReadOnlyList<LinqParameter>? parameters = null)
        => (await LinqCompiler.GetLanguageService(Globals(model, parameters)).GetCompletionsAsync(query, caret)).Items;

    public static Task<HoverInfo?> HoverAsync(LinqGeneratedModelAssembly? model, string query, int offset)
        => LinqCompiler.GetLanguageService(Globals(model)).GetHoverAsync(query, offset);

    public static Task<IReadOnlyList<CodeDiagnostic>> DiagnosticsAsync(LinqGeneratedModelAssembly? model, string query)
        => LinqCompiler.GetLanguageService(Globals(model)).GetDiagnosticsAsync(query);

    public static Task<SignatureHelpInfo?> SignatureHelpAsync(LinqGeneratedModelAssembly? model, string query, int offset)
        => LinqCompiler.GetLanguageService(Globals(model)).GetSignatureHelpAsync(query, offset);
}
