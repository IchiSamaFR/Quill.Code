using Microsoft.CodeAnalysis.CSharp;
using Quill.Data.Sql;
using System.Globalization;
using System.Text;
using static Quill.Code.CSharp.Linq.Constants.EfModelConstants;

namespace Quill.Code.CSharp.Linq.Modeling;

/// <summary>
/// Genere le code C# d'un DbContext EF Core a partir de la structure d'une base : une classe par
/// table ou vue, un DbSet par classe, et le mapping (tables, cles, noms de colonnes, precisions).
/// </summary>
internal static class EfModelGenerator
{
    /// <summary>Noms que l'entite ne peut pas prendre : ils masqueraient les types utilises par le code genere.</summary>
    private static readonly HashSet<string> ReservedTypeNames = new(StringComparer.Ordinal)
    {
        "DbContext", "DbSet", "DbContextOptions", "ModelBuilder", "System", "Microsoft", "String", "DateTime",
        "DateOnly", "TimeSpan", "DateTimeOffset", "Guid", "Decimal", "Object", ExtensionsClassName
    };

    public static EfGeneratedModel Generate(SqlDatabaseSchema snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        var contextClass = ToIdentifier(snapshot.Name.TrimStart('@') + "Context");

        var entities = BuildEntities(snapshot, contextClass);
        var source = WriteSource(contextClass, entities);

        return new EfGeneratedModel(GeneratedNamespace, contextClass, source, entities.Select(e => e.ClassName).ToArray());
    }

    // ---------- Modele intermediaire ----------

    private sealed record EntityModel(SqlSchemaObject Source, string ClassName, IReadOnlyList<PropertyModel> Properties);

    private sealed record PropertyModel(SqlColumn Source, string Name, string ClrType);

    private static List<EntityModel> BuildEntities(SqlDatabaseSchema snapshot, string contextClass)
    {
        var dataSources = snapshot.Objects
            .Where(o => o.Kind is SqlSchemaObjectKind.Table or SqlSchemaObjectKind.View)
            .OrderBy(o => o.Schema, StringComparer.OrdinalIgnoreCase)
            .ThenBy(o => o.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        // Meme nom dans deux schemas : le schema prefixe le nom des deux (Sales_Orders, dbo_Orders).
        var ambiguous = dataSources.GroupBy(o => ToIdentifier(o.Name), StringComparer.Ordinal)
                                   .Where(g => g.Count() > 1)
                                   .Select(g => g.Key)
                                   .ToHashSet(StringComparer.Ordinal);

        var usedNames = new HashSet<string>(StringComparer.Ordinal) { contextClass };
        List<EntityModel> entities = [];

        foreach (var source in dataSources)
        {
            var baseName = ToIdentifier(source.Name);
            if (ambiguous.Contains(baseName) || ReservedTypeNames.Contains(baseName.TrimStart('@')))
                baseName = ToIdentifier($"{source.Schema}_{source.Name}");

            var className = Unique(baseName, usedNames);
            var properties = BuildProperties(source, className);
            if (properties.Count == 0)
            {
                usedNames.Remove(className.TrimStart('@'));
                continue;
            }

            entities.Add(new EntityModel(source, className, properties));
        }

        return entities;
    }

    private static List<PropertyModel> BuildProperties(SqlSchemaObject source, string className)
    {
        // Le nom de la classe est reserve : une propriete homonyme est interdite en C# (CS0542).
        var usedNames = new HashSet<string>(StringComparer.Ordinal) { className };
        List<PropertyModel> properties = [];

        foreach (var column in source.Columns)
        {
            // sql_variant, hierarchyid, geography... : pas de mapping EF sans paquet dedie, la colonne est ignoree.
            if (ToClrType(column) is not { } clrType)
                continue;

            properties.Add(new PropertyModel(column, Unique(ToIdentifier(column.Name), usedNames), clrType));
        }

        return properties;
    }

    /// <summary>Type .NET du mapping EF Core SQL Server par defaut ; null si le type n'est pas mappable.</summary>
    private static string? ToClrType(SqlColumn column)
    {
        var sqlType = (string.IsNullOrEmpty(column.SqlType) ? column.DataType : column.SqlType).ToLowerInvariant();
        var parenthesis = sqlType.IndexOf('(', StringComparison.Ordinal);
        if (parenthesis > 0)
            sqlType = sqlType[..parenthesis];

        var (type, isValueType) = sqlType switch
        {
            "bigint" => ("long", true),
            "int" => ("int", true),
            "smallint" => ("short", true),
            "tinyint" => ("byte", true),
            "bit" => ("bool", true),
            "decimal" or "numeric" or "money" or "smallmoney" => ("decimal", true),
            "float" => ("double", true),
            "real" => ("float", true),
            "date" => ("DateOnly", true),
            "time" => ("TimeSpan", true),
            "datetime" or "datetime2" or "smalldatetime" => ("DateTime", true),
            "datetimeoffset" => ("DateTimeOffset", true),
            "uniqueidentifier" => ("Guid", true),
            "char" or "varchar" or "nchar" or "nvarchar" or "text" or "ntext" or "xml" or "sysname" => ("string", false),
            "binary" or "varbinary" or "image" or "timestamp" or "rowversion" => ("byte[]", false),
            _ => (null, false)
        };

        if (type is null)
            return null;

        return column.IsNullable ? type + "?" : type;
    }

    // ---------- Ecriture du source ----------

    private static string WriteSource(string contextClass, List<EntityModel> entities)
    {
        var code = new StringBuilder();
        code.AppendLine("// <auto-generated> Modele EF genere depuis le catalogue de la base. </auto-generated>");
        code.AppendLine("#nullable enable");
        code.AppendLine("using System;");
        code.AppendLine("using Microsoft.EntityFrameworkCore;");
        code.AppendLine();
        code.AppendLine(CultureInfo.InvariantCulture, $"namespace {GeneratedNamespace};");
        code.AppendLine();

        foreach (var entity in entities)
            WriteEntity(code, entity);

        code.AppendLine(CultureInfo.InvariantCulture, $"public sealed class {contextClass} : DbContext");
        code.AppendLine("{");
        code.AppendLine(CultureInfo.InvariantCulture, $"    public {contextClass}(DbContextOptions<{contextClass}> options = null) : base(options) {{ }}");
        code.AppendLine();

        foreach (var entity in entities)
            // base. : une table nommee Set, Model ou Database masquerait le membre herite.
            code.AppendLine(CultureInfo.InvariantCulture, $"    public DbSet<{entity.ClassName}> {entity.ClassName} => base.Set<{entity.ClassName}>();");

        code.AppendLine();
        code.AppendLine("    protected override void OnModelCreating(ModelBuilder modelBuilder)");
        code.AppendLine("    {");

        foreach (var entity in entities)
            WriteMapping(code, entity);

        code.AppendLine("    }");
        code.AppendLine("}");
        code.AppendLine();
        WriteExtensions(code);

        return code.ToString();
    }

    /// <summary>Dump() facon LINQPad : affiche une valeur dans son propre onglet et la rend telle quelle.</summary>
    private static void WriteExtensions(StringBuilder code)
    {
        code.AppendLine(CultureInfo.InvariantCulture, $"public static class {ExtensionsClassName}");
        code.AppendLine("{");
        code.AppendLine("    private static readonly global::System.Threading.AsyncLocal<Action<object?, string?>?> __dumpSink = new();");
        code.AppendLine();
        code.AppendLine(CultureInfo.InvariantCulture, $"    private static Action<object?, string?>? {DumpSinkPropertyName} {{ get => __dumpSink.Value; set => __dumpSink.Value = value; }}");
        code.AppendLine();
        code.AppendLine("    public static T Dump<T>(this T value, string? title = null)");
        code.AppendLine("    {");
        code.AppendLine(CultureInfo.InvariantCulture, $"        {DumpSinkPropertyName}?.Invoke(value, title);");
        code.AppendLine("        return value;");
        code.AppendLine("    }");
        code.AppendLine("}");
    }

    private static void WriteEntity(StringBuilder code, EntityModel entity)
    {
        // [Description] : lu par l'infobulle de l'editeur LINQ (la doc XML ne survit pas a la compilation en memoire).
        code.AppendLine(CultureInfo.InvariantCulture, $"[global::System.ComponentModel.Description({Literal(DescribeSource(entity.Source))})]");
        code.AppendLine(CultureInfo.InvariantCulture, $"public sealed class {entity.ClassName}");
        code.AppendLine("{");

        foreach (var property in entity.Properties)
        {
            code.AppendLine(CultureInfo.InvariantCulture, $"    [global::System.ComponentModel.Description({Literal(DescribeColumn(entity.Source, property.Source))})]");
            // Une chaine non nulle part a null! : EF la renseigne a la lecture.
            var initializer = property.ClrType is "string" or "byte[]" ? " = null!;" : string.Empty;
            code.AppendLine(CultureInfo.InvariantCulture, $"    public {property.ClrType} {property.Name} {{ get; set; }}{initializer}");
        }

        code.AppendLine("}");
        code.AppendLine();
    }

    private static void WriteMapping(StringBuilder code, EntityModel entity)
    {
        var source = entity.Source;

        code.AppendLine(CultureInfo.InvariantCulture, $"        modelBuilder.Entity<{entity.ClassName}>(e =>");
        code.AppendLine("        {");

        code.AppendLine(source.Kind == SqlSchemaObjectKind.View
            ? $"            e.ToView({Literal(source.Name)}, {Literal(source.Schema)});"
            : $"            e.ToTable({Literal(source.Name)}, {Literal(source.Schema)});");

        var keys = entity.Properties.Where(p => p.Source.IsPrimaryKey).OrderBy(p => p.Source.PrimaryKeyOrdinal).ToArray();

        // Une colonne de cle ignoree (type non mappable) rendrait la cle fausse : on la traite comme absente.
        if (keys.Length != source.Columns.Count(c => c.IsPrimaryKey))
            keys = [];

        code.AppendLine(keys.Length switch
        {
            // Vue ou table sans cle : lecture seule, sans suivi.
            0 => "            e.HasNoKey();",
            1 => $"            e.HasKey(x => x.{keys[0].Name});",
            _ => $"            e.HasKey(x => new {{ {string.Join(", ", keys.Select(k => "x." + k.Name))} }});"
        });

        foreach (var property in entity.Properties)
        {
            var column = property.Source;
            var configuration = new StringBuilder();

            if (property.Name.TrimStart('@') != column.Name)
                configuration.Append(CultureInfo.InvariantCulture, $".HasColumnName({Literal(column.Name)})");

            // Sans precision explicite, EF tronque les decimales a (18,2) et le signale a chaque requete.
            if (property.ClrType.StartsWith("decimal", StringComparison.Ordinal) && column.Precision > 0)
                configuration.Append(CultureInfo.InvariantCulture, $".HasPrecision({column.Precision}, {column.Scale})");

            if (configuration.Length > 0)
                code.AppendLine(CultureInfo.InvariantCulture, $"            e.Property(x => x.{property.Name}){configuration};");
        }

        code.AppendLine("        });");
        code.AppendLine();
    }

    // ---------- Identifiants ----------

    /// <summary>Identifiant C# valide : caracteres interdits -> _, chiffre initial prefixe, mot-cle echappe par @.</summary>
    public static string ToIdentifier(string name)
    {
        ArgumentNullException.ThrowIfNull(name);

        var builder = new StringBuilder(name.Length + 1);
        foreach (var c in name)
            builder.Append(SyntaxFacts.IsIdentifierPartCharacter(c) ? c : '_');

        if (builder.Length == 0 || !SyntaxFacts.IsIdentifierStartCharacter(builder[0]))
            builder.Insert(0, '_');

        var identifier = builder.ToString();
        return SyntaxFacts.GetKeywordKind(identifier) != SyntaxKind.None ? "@" + identifier : identifier;
    }

    private static string Unique(string name, HashSet<string> usedNames)
    {
        var candidate = name;
        for (var index = 2; !usedNames.Add(candidate.TrimStart('@')); index++)
            candidate = $"{name.TrimStart('@')}{index}";

        return candidate;
    }

    /// <summary>Ex. "Table dbo.Student".</summary>
    private static string DescribeSource(SqlSchemaObject source)
        => $"{(source.Kind == SqlSchemaObjectKind.View ? "Vue" : "Table")} {source.Schema}.{source.Name}";

    /// <summary>Ex. "Colonne dbo.Student.Name · nvarchar(50) · not null · clé primaire".</summary>
    private static string DescribeColumn(SqlSchemaObject source, SqlColumn column)
    {
        var description = $"Colonne {source.Schema}.{source.Name}.{column.Name} · {column.DataType} · {(column.IsNullable ? "null" : "not null")}";
        return column.IsPrimaryKey ? description + " · clé primaire" : description;
    }

    /// <summary>Litteral C# echappe par Roslyn (guillemets, antislash, retours a la ligne, caracteres de controle).</summary>
    private static string Literal(string value) => SymbolDisplay.FormatLiteral(value, quote: true);
}
