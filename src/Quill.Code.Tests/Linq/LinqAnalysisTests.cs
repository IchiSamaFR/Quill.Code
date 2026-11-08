using Quill.Code.CSharp.Linq.Compilation;
using Quill.Code.Analysis;

namespace Quill.Code.Tests.Linq;

/// <summary>The | character marks the position (mouse or caret); Roslyn analyzes in the generated context.</summary>
public class LinqAnalysisTests
{
    private static Lazy<LinqGeneratedModelAssembly> Model => TestLinq.Model;

    [Fact]
    public async Task Hover_PropertyShowsTypeAndColumn()
    {
        var hover = await HoverAsync(TestLinq.Expression("Db.AspNetUsers.Where(u => u.Em|ail != null)"));

        Assert.NotNull(hover);
        Assert.Contains("string", hover.Signature, StringComparison.Ordinal);
        Assert.Contains("AspNetUsers.Email", hover.Signature, StringComparison.Ordinal);
        Assert.Contains("nvarchar", hover.Description, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Hover_LambdaParameterShowsEntity()
    {
        var query = TestLinq.Expression("Db.AspNetUsers.Where(u => u.Email != null)");
        var hover = await TestLinq.HoverAsync(Model.Value, query, query.IndexOf("u =>", StringComparison.Ordinal));

        Assert.NotNull(hover);
        Assert.Contains("AspNetUsers u", hover.Signature, StringComparison.Ordinal);
        Assert.Contains("dbo.AspNetUsers", hover.Description, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Hover_LocalVariableShowsInferredType()
    {
        var hover = await HoverAsync(TestLinq.Statements("var us|ers = Db.AspNetUsers.Where(u => u.EmailConfirmed);\nusers.Dump();"));

        Assert.NotNull(hover);
        // Nullable context enabled: a var is annotated (IQueryable<AspNetUsers>?).
        Assert.Matches(@"IQueryable<AspNetUsers>\?? users", hover.Signature);
    }

    [Fact]
    public async Task Hover_RangeMapsToQueryText()
    {
        var query = TestLinq.Expression("Db.AspNetUsers");
        var hover = await TestLinq.HoverAsync(Model.Value, query, query.IndexOf("AspNetUsers", StringComparison.Ordinal) + "AspNetUsers".Length - 2);

        Assert.NotNull(hover);
        Assert.Equal(query.IndexOf("AspNetUsers", StringComparison.Ordinal), hover.Start);
        Assert.Equal("AspNetUsers".Length, hover.Length);
    }

    [Fact]
    public async Task Hover_NothingOnPunctuation()
        => Assert.Null(await HoverAsync(TestLinq.Expression("Db.AspNetUsers.Where(u => u.EmailConfirmed |)")));

    [Fact]
    public async Task Diagnostics_UnknownMemberIsAnError()
    {
        var query = TestLinq.Expression("Db.AspNetUsers.Where(u => u.Mail != null)");
        var diagnostics = await TestLinq.DiagnosticsAsync(Model.Value, query);

        var error = Assert.Single(diagnostics);
        Assert.Equal(CodeDiagnosticSeverity.Error, error.Severity);
        Assert.Equal("Mail", query.Substring(error.Start, error.Length));
    }

    [Fact]
    public async Task Diagnostics_ValidQueryHasNone()
        => Assert.Empty(await TestLinq.DiagnosticsAsync(Model.Value, TestLinq.Expression("Db.AspNetUsers.Where(u => u.EmailConfirmed).Select(u => u.Email)")));

    [Fact]
    public async Task Diagnostics_MissingParenthesis()
    {
        var diagnostics = await TestLinq.DiagnosticsAsync(Model.Value, TestLinq.Expression("Db.AspNetUsers.Where(u => u.EmailConfirmed"));

        Assert.Contains(diagnostics, d => d.IsError);
    }

    [Fact]
    public async Task Diagnostics_WithoutModelOnlySyntax()
    {
        Assert.Empty(await TestLinq.DiagnosticsAsync(null, TestLinq.Expression("Db.AspNetUsers.Where(u => u.EmailConfirmed)")));
        Assert.NotEmpty(await TestLinq.DiagnosticsAsync(null, TestLinq.Expression("Db.AspNetUsers.Where(u => )")));
    }

    [Fact]
    public async Task Signature_WhereWithActiveParameter()
    {
        var help = await SignatureAsync(TestLinq.Expression("Db.AspNetUsers.Where(|"));

        Assert.NotNull(help);
        Assert.Equal(0, help.ActiveParameter);
        Assert.Contains(help.Signatures, s => s.Prefix.Contains("Where", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Signature_SecondArgument()
    {
        var help = await SignatureAsync(TestLinq.Expression("Db.AspNetUsers.Select(u => u.Email).Take(10).Skip(|"));

        Assert.NotNull(help);
        Assert.Contains(help.Signatures, s => s.Prefix.Contains("Skip", StringComparison.Ordinal));

        var second = await SignatureAsync(TestLinq.Expression("Db.AspNetUsers.Where(u => u.Email.StartsWith(\"a\", |"));
        Assert.NotNull(second);
        Assert.Equal(1, second.ActiveParameter);
        Assert.Contains(second.Signatures, s => s.Prefix.Contains("StartsWith", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Signature_NoneOutsideArguments()
        => Assert.Null(await SignatureAsync(TestLinq.Expression("Db.AspNetUsers.Count()|")));

    private static Task<HoverInfo?> HoverAsync(string queryWithMarker)
    {
        var offset = queryWithMarker.IndexOf('|', StringComparison.Ordinal);
        return TestLinq.HoverAsync(Model.Value, queryWithMarker.Remove(offset, 1), offset);
    }

    private static Task<SignatureHelpInfo?> SignatureAsync(string queryWithCaret)
    {
        var caret = queryWithCaret.IndexOf('|', StringComparison.Ordinal);
        return TestLinq.SignatureHelpAsync(Model.Value, queryWithCaret.Remove(caret, 1), caret);
    }
}
