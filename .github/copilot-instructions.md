# Quill.Code - Instructions for GitHub Copilot

.NET library of the building blocks shared by code editors (highlighting, completion, diagnostics, editing), with no dependency on a UI framework. Consumed by Querify (MAUI) and PocketForge (WPF).

**The full instructions, shared with Claude, are in [`.github/instructions.md`](instructions.md)**: read them before any change; they take precedence over this summary.

Key points:
1. No reference to WPF, MAUI or any UI type: the library produces data, the apps display it.
2. `Quill.Code` ← `Quill.Code.[Language]`; a language never references another language. `Quill.Data.Sql` (database structure, no dependency) ← `Quill.Data.Sql.[Engine]` (catalog reading) and ← `Quill.Code.Sql.SqlServer` and `Quill.Code.CSharp.Linq` (← `Quill.Code.CSharp`, engine-agnostic) ← `Quill.Code.CSharp.Linq.[Engine]` (`ILinqDatabaseProvider`); `Quill.Code` ← `Quill.Code.Sql` (`ISqlQueryExecutor` contract, `DbQueryExecutor` base) ← `Quill.Code.Sql.[Engine]` (SQL execution; plus T-SQL for `Quill.Code.Sql.SqlServer`); a `Quill.Code.*` never references a `Quill.Data.Sql.[Engine]`. Engines: SqlServer, MySql, PostgreSql, Oracle, Sqlite. Exception: `Quill.Code.Sql.[Engine]` and `Quill.Code.CSharp.Linq.[Engine]` (net10.0) depend on a driver for execution (`ISqlQueryExecutor` from `Quill.Code.Sql`, `LinqQueryExecutor` on an `ILinqDatabaseProvider`, same return contract); analysis and completion stay connection-free.
3. No dependency injection in Quill (no container, no static registry): apps instantiate the services (`new CSharpLanguageService(() => options)`) or register them in their own container.
4. Reuse before creating: an existing type (from `Quill.Code` first), extended if needed, rather than a new class.
5. One type per file, `internal` / `sealed` by default, XML documentation on the public API; everything (code, comments, docs, commits) in English.
6. No constant declared in a working class: they go in the project's `Constants/` folder (namespace `<Project>.Constants`), one static class per context (`SqlServerConstants`, `EditingConstants`, `SqlServerQueries`…), `internal` unless it is part of the API.
7. Package versions only in `src/Directory.Packages.props`.
8. Commits: `type: description` in English, no trailers (`Co-Authored-By`, `Signed-off-by`…).

Build: `dotnet build src/Quill.Code.slnx` — Test: `dotnet test src/Quill.Code.slnx`
