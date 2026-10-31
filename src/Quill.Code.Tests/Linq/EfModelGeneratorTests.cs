using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Quill.Code.CSharp.Linq.Constants;
using Quill.Code.CSharp.Linq.Modeling;
using System.Reflection;
using Quill.Data.Sql;

namespace Quill.Code.Tests.Linq;

/// <summary>
/// Le code genere est compile avec Roslyn puis instancie : le modele EF est construit
/// (validation comprise) sans se connecter, ToQueryString n'ouvre pas de connexion.
/// </summary>
public class EfModelGeneratorTests
{
    private static readonly Lazy<(EfGeneratedModel Model, Type ContextType)> Generated = new(Build);

    [Fact]
    public void Context_NamedAfterDatabase()
    {
        var model = Generated.Value.Model;

        Assert.Equal("RaveOnDbContext", model.ContextClassName);
    }

    [Fact]
    public void Entities_NamesAreValidAndUnique()
    {
        var names = Generated.Value.Model.EntityClassNames;

        Assert.Contains("AspNetUsers", names);
        Assert.Contains("dbo_Orders", names);
        Assert.Contains("Sales_Orders", names);
        Assert.Contains("Order_Lines", names);
        Assert.Contains("_1stTable", names);
        Assert.DoesNotContain(names, n => n.Contains("Variants", StringComparison.Ordinal));
    }

    [Fact]
    public void Table_MappedWithSchemaAndKey()
    {
        var users = Entity("AspNetUsers");

        Assert.Equal("AspNetUsers", users.GetTableName());
        Assert.Equal("dbo", users.GetSchema());
        Assert.Equal(new[] { "Id" }, users.FindPrimaryKey()!.Properties.Select(p => p.Name));
        Assert.True(users.FindProperty("Email")!.IsNullable);
        Assert.False(users.FindProperty("EmailConfirmed")!.IsNullable);
    }

    [Fact]
    public void Table_CompositeKeyInOrdinalOrder()
    {
        var roles = Entity("AspNetUserRoles");

        Assert.Equal(new[] { "UserId", "RoleId" }, roles.FindPrimaryKey()!.Properties.Select(p => p.Name));
    }

    [Fact]
    public void Table_IrregularNamesKeywordsAndUnmappableColumns()
    {
        var lines = Entity("Order_Lines");

        Assert.Equal("Order Lines", lines.GetTableName());
        Assert.Equal("Sales", lines.GetSchema());
        Assert.Null(lines.FindPrimaryKey());

        var price = lines.FindProperty("Unit_Price")!;
        Assert.Equal("Unit Price", price.GetColumnName());
        Assert.Equal(18, price.GetPrecision());
        Assert.Equal(4, price.GetScale());

        Assert.Equal("class", lines.FindProperty("class")!.GetColumnName());
        Assert.Equal("Order Lines", lines.FindProperty("Order_Lines2")!.GetColumnName());
        Assert.Null(lines.FindProperty("Location"));
    }

    [Fact]
    public void View_IsKeyless()
    {
        var view = Entity("vActiveUsers");

        Assert.Equal("vActiveUsers", view.GetViewName());
        Assert.Null(view.FindPrimaryKey());
    }

    [Fact]
    public void Query_TranslatesToSql()
    {
        using var context = CreateContext();
        var users = (IQueryable)context.GetType().GetProperty("AspNetUsers")!.GetValue(context)!;

        Assert.Contains("[AspNetUsers]", users.ToQueryString(), StringComparison.Ordinal);
    }

    // ---------- Construction ----------

    private static IEntityType Entity(string className)
    {
        using var context = CreateContext();
        return context.Model.FindEntityType($"{EfModelConstants.GeneratedNamespace}.{className}")
            ?? throw new InvalidOperationException($"Entite {className} absente du modele.");
    }

    private static DbContext CreateContext()
    {
        var contextType = Generated.Value.ContextType;
        var builder = (DbContextOptionsBuilder)Activator.CreateInstance(typeof(DbContextOptionsBuilder<>).MakeGenericType(contextType))!;
        builder.UseSqlServer("Server=localhost;Database=RaveOnDb;Trusted_Connection=True;TrustServerCertificate=True");

        return (DbContext)Activator.CreateInstance(contextType, builder.Options)!;
    }

    private static (EfGeneratedModel, Type) Build()
    {
        var model = EfModelGenerator.Generate(SampleSnapshot());

        var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator)
            .Select(path => MetadataReference.CreateFromFile(path));

        var compilation = CSharpCompilation.Create(
            "GeneratedModel",
            [CSharpSyntaxTree.ParseText(model.Source)],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));

        using var image = new MemoryStream();
        var result = compilation.Emit(image);

        var errors = result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
        Assert.True(result.Success, string.Join(Environment.NewLine, errors.Select(e => e.ToString())) + Environment.NewLine + model.Source);

        var assembly = Assembly.Load(image.ToArray());
        return (model, assembly.GetType($"{model.Namespace}.{model.ContextClassName}", throwOnError: true)!);
    }

    /// <summary>Catalogue de test, partage avec <see cref="LinqCompilerTests"/>.</summary>
    internal static SqlDatabaseSchema SampleSnapshot() => new("RaveOnDb",
        [
            Table("dbo", "AspNetUsers",
                Column("Id", "nvarchar", nullable: false, key: 1),
                Column("Email", "nvarchar"),
                Column("EmailConfirmed", "bit", nullable: false),
                Column("AccessFailedCount", "int", nullable: false),
                Column("LockoutEnd", "datetimeoffset")),
            Table("dbo", "AspNetUserRoles",
                Column("RoleId", "nvarchar", nullable: false, key: 2),
                Column("UserId", "nvarchar", nullable: false, key: 1)),
            Table("dbo", "Orders", Column("Id", "int", nullable: false, key: 1)),
            Table("Sales", "Orders", Column("Id", "int", nullable: false, key: 1), Column("Total", "money", nullable: false)),
            Table("Sales", "Order Lines",
                Column("Unit Price", "decimal", nullable: false, precision: 18, scale: 4),
                Column("class", "int"),
                Column("Order Lines", "nvarchar"),
                Column("Location", "geography")),
            Table("dbo", "1stTable", Column("Code", "char", nullable: false, key: 1), Column("CreatedOn", "date"), Column("Duration", "time")),
            Table("dbo", "Variants", Column("Value", "sql_variant")),
            new SqlSchemaObject("dbo", "vActiveUsers", SqlSchemaObjectKind.View, [Column("Id", "nvarchar", nullable: false), Column("Email", "nvarchar")]),
            new SqlSchemaObject("dbo", "usp_Purge", SqlSchemaObjectKind.Procedure, []),
        ]);

    private static SqlSchemaObject Table(string schema, string name, params SqlColumn[] columns)
        => new(schema, name, SqlSchemaObjectKind.Table, columns);

    private static SqlColumn Column(string name, string sqlType, bool nullable = true, int key = 0, byte precision = 0, byte scale = 0)
        => new(name, sqlType) { SqlType = sqlType, IsNullable = nullable, PrimaryKeyOrdinal = key, Precision = precision, Scale = scale };
}
