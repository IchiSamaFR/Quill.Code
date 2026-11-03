using Quill.Code.CSharp.Linq.Compilation;
using Quill.Code.CSharp.Linq.SqlServer;
using Quill.Code.CSharp.Linq.Execution;
using Quill.Code.Analysis;
using Quill.Code.Completion;
using Quill.Data.Sql;
using System.Collections.Concurrent;

namespace Quill.Code.Tests.Linq;

/// <summary>
/// Analyse d'une requete dans son contexte genere, comme le fait l'editeur LINQ une fois le DbContext resolu
/// (services de <see cref="LinqCompiler"/>, partages : la composition MEF est couteuse).
/// </summary>
internal static class TestLinq
{
    /// <summary>Moteur des tests, une seule instance : les DbContext compiles sont mis en cache par fournisseur.</summary>
    public static SqlServerLinqProvider Provider { get; } = new("Quill.Code.Tests");

    /// <summary>Structure de test, une seule instance : son modele compile est partage par toutes les classes (cache de LinqContexts).</summary>
    public static SqlDatabaseSchema Schema { get; } = EfModelGeneratorTests.SampleSnapshot();

    /// <summary>DbContext compile de <see cref="Schema"/>.</summary>
    public static Lazy<LinqGeneratedModelAssembly> Model { get; } = new(() => LinqContexts.GetModelAsync(Provider, Schema).GetAwaiter().GetResult());

    /// <summary>Requete C# native dont <paramref name="expression"/> est la valeur d'une variable ; le contexte s'appelle Db.</summary>
    public static string Expression(string expression) => $"var result = {expression};\n";

    /// <summary>Requete C# native faite des instructions <paramref name="statements"/>.</summary>
    public static string Statements(string statements) => statements + "\n";

    /// <summary>QueryGlobals compiles une fois par modele et jeu de parametres (noms et types), comme dans LinqContexts.</summary>
    private static readonly ConcurrentDictionary<(LinqGeneratedModelAssembly?, string), Lazy<LinqParametersAssembly>> GlobalsCache = new();

    /// <summary>QueryGlobals de <paramref name="model"/> (null = C# seul) et des <paramref name="parameters"/>.</summary>
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
