# Quill.Code

Shared building blocks for .NET code editors, independent of the UI framework (WPF, MAUI…): syntax highlighting, completion, diagnostics and editing algorithms.

## Packages

| Package | Contents |
|---|---|
| `Quill.Code` | Models, contracts, reading of `.xshd` definitions, splitting into colored spans, editing algorithms. No UI dependency. |
| `Quill.Code.CSharp` | C# via Roslyn: completion, diagnostics, hover, signature help, compilation and loading. |
| `Quill.Code.CSharp.Linq` | LINQ / Entity Framework Core (net10.0), engine-agnostic: DbContext generated from a database structure (`Quill.Data.Sql`), C# completion, diagnostics and hover in that context (`LinqLanguageService`), execution (`LinqQueryExecutor`, same contract as `SqlServerQueryExecutor`); the engine is an `ILinqDatabaseProvider`. |
| `Quill.Code.CSharp.Linq.SqlServer` | `SqlServerLinqProvider`: LINQ on SQL Server (SQL Server types, EF Core SqlServer, Microsoft.Data.SqlClient). |
| `Quill.Code.CSharp.Linq.MySql` | `MySqlLinqProvider`: LINQ on MySQL / MariaDB (Microting.EntityFrameworkCore.MySql, EF Core 10 fork of Pomelo, MySqlConnector). |
| `Quill.Code.CSharp.Linq.PostgreSql` | `PostgreSqlLinqProvider`: LINQ on PostgreSQL (Npgsql.EntityFrameworkCore.PostgreSQL). |
| `Quill.Code.CSharp.Linq.Oracle` | `OracleLinqProvider`: LINQ on Oracle (Oracle.EntityFrameworkCore, 19c-compatible SQL). |
| `Quill.Code.CSharp.Linq.Sqlite` | `SqliteLinqProvider`: LINQ on SQLite (Microsoft.EntityFrameworkCore.Sqlite). |
| `Quill.Code.Sql` | Foundation of SQL execution: `ISqlQueryExecutor` contract and ADO.NET base (`DbQueryExecutor`, `DbResultSetReader`). No dependency. |
| `Quill.Code.Sql.SqlServer` | T-SQL: lexer, input context analysis (what the grammar expects at the caret, tables and aliases of the batch, database of the `USE`), syntax diagnostics through ScriptDom, completion and hover on the catalog provided by the host (`ISqlCatalog`); execution on SQL Server (`SqlServerQueryExecutor`). |
| `Quill.Code.Sql.MySql` / `.PostgreSql` / `.Oracle` / `.Sqlite` | SQL execution on each engine (`MySqlQueryExecutor`, `PostgreSqlQueryExecutor`, `OracleQueryExecutor` — script split as in SQL*Plus —, `SqliteQueryExecutor`), on `DbQueryExecutor`. |
| `Quill.Data.Sql` | Structure of a database (databases, schemas, tables, views, procedures, columns) and catalog reading contract (`ISqlSchemaReader`). No dependency. |
| `Quill.Data.Sql.SqlServer` | Reads the SQL Server catalog (`sys.*`) via Microsoft.Data.SqlClient. |
| `Quill.Data.Sql.MySql` / `.PostgreSql` / `.Oracle` / `.Sqlite` | Catalog reading: `information_schema` (MySqlConnector), `pg_catalog` (Npgsql), `ALL_*` views (ODP.NET Core), `sqlite_master` and pragmas (Microsoft.Data.Sqlite). |

Display is up to the application: each app maps colored spans, suggestions and diagnostics to its own controls.

## Installation

Packages are published on [nuget.org](https://www.nuget.org/profiles/IchiSamaFR):

```
dotnet add package QuillCode.CSharp
```

Since the `Quill.` prefix is reserved on nuget.org, each `Quill.Code.X` project is published under the ID `QuillCode.X` and `Quill.Data.Sql.X` under `QuillCode.Data.Sql.X`; assemblies and namespaces keep their names.

## Usage

One service per context, with its own Roslyn project, created on first request (off the UI thread):

```csharp
var language = new CSharpLanguageService(() => new CSharpLanguageOptions
{
    Name = "Script",
    References = CSharpReferences.FromTrustedPlatformAssemblies(),
    CompilationOptions = new(OutputKind.ConsoleApplication),
    AdditionalDocuments = new Dictionary<string, string> { ["GlobalUsings.cs"] = "global using System;" },
    SourceTransform = code => code,   // optional: analyzed source, same length as the text
    TextReferences = code => [],      // optional: references declared by the text
    IncludeSemanticDiagnostics = true, // false: syntax errors only
});

var completions = await language.GetCompletionsAsync(code, caret);
var change = await language.GetChangeAsync(code, completions.Items[0]);
var diagnostics = await language.GetDiagnosticsAsync(code);
```

`CSharpLanguageService` implements `ICodeLanguageService`: an editor control binds to it without depending on Roslyn. Without options, the service analyzes a library that references the framework.

Compiling the same project as the editor, loaded into a collectible context (calling the code is up to the application):

```csharp
using var compiled = await language.CompileAsync(code, new CSharpCompileOptions
{
    AssemblyFiles = packageAssemblies,     // loadable assemblies, read into memory
});

if (!compiled.Success)
    foreach (var error in compiled.Errors)
        Console.WriteLine(error.Describe(code)); // "Line 2, column 6: CS0103 …"
else
    Run(compiled.Assembly!);
```

A script (entry point or top-level statements) compiled and run in one call, with no return value: output goes through `Log`, the assembly is unloaded at the end.

```csharp
try
{
    await language.RunAsync(code, new CSharpCompileOptions { Log = e => Console.WriteLine($"{e.Timestamp:HH:mm:ss} {e.Level} : {e.Text}") }, token);
}
catch (CSharpCompilationException ex)
{
    foreach (var error in ex.Errors)
        Console.WriteLine(error.Describe(code));
}
```

Positions received and returned are always those of the editor text.

Sources added on the fly (e.g. a helper class): visible to completion and analysis, compiled with the text.

```csharp
language.SetSource("FileHelper.cs", helperCode);                       // name → code
var key = await language.SetSourceFileAsync(@"C:\Helpers\FileHelper.cs"); // read again on each call
language.RemoveSource(key);
language.SourcesChanged += (_, _) => RefreshDiagnostics();
```

An error in a source is returned at position 0 with the file: "FileHelper.cs (12, 5): …".

### T-SQL

Quill connects to no database: the host provides an `ISqlCatalog` (databases, then the complete schema of a database, columns included), or an in-memory `SqlCatalog`.

```csharp
var language = new SqlLanguageService(new SqlLanguageOptions
{
    Catalog = new SqlCatalog([new SqlDatabaseSchema("Shop", objects)]), // or the host's implementation
    Database = "Shop",                                                   // null: default database of the catalog
    Variables = [new SqlVariable("@MaxRows", "int", "Parameter")],       // known without DECLARE
});

var completions = await language.GetCompletionsAsync("SELECT u.| FROM dbo.Users u", caret); // columns of Users
```

Without a catalog, completion suggests keywords, document words and variables. An unavailable catalog returns an empty list (or null); an exception is treated as a missing schema.

## Development

```
dotnet build src/Quill.Code.slnx
dotnet test src/Quill.Code.slnx
```

Publishing a version on nuget.org: `git tag v0.1.0 && git push origin v0.1.0` (pre-release: `v0.2.0-beta.1`), then bump `VersionPrefix` in `src/Directory.Build.props`.

## License

MIT
