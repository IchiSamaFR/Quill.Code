using Quill.Code.Completion;
using Quill.Code.Sql.Dialects;
using Quill.Code.Sql.SqlServer.Schema;
using Quill.Data.Sql;

namespace Quill.Code.Sql.SqlServer.Completion;

/// <summary>
/// Databases, schemas, tables, views, procedures and columns of the catalog, depending on the context:
/// FROM → tables, alias. → columns, USE → databases, EXEC → procedures.
/// </summary>
internal sealed class SqlSchemaCompletion
{
    private static readonly SqlSchemaObjectKind[] DataSourceKinds = [SqlSchemaObjectKind.Table, SqlSchemaObjectKind.View];

    private static readonly SqlSchemaObjectKind[] ProcedureKinds = [SqlSchemaObjectKind.Procedure];

    private readonly SqlCatalogReader _catalog;
    private readonly SqlDialect _dialect;

    public SqlSchemaCompletion(SqlCatalogReader catalog, SqlDialect dialect)
    {
        _catalog = catalog;
        _dialect = dialect;
    }

    public async Task<IReadOnlyList<CompletionItem>> GetItemsAsync(SqlContext context, CancellationToken cancellationToken)
    {
        if (!_catalog.IsAvailable)
            return [];

        return context.Kind switch
        {
            SqlContextKind.Database => await GetDatabaseItemsAsync(cancellationToken).ConfigureAwait(false),
            SqlContextKind.TableSource => await GetSourceItemsAsync(context.Database, DataSourceKinds, cancellationToken).ConfigureAwait(false),
            SqlContextKind.Procedure => await GetSourceItemsAsync(context.Database, ProcedureKinds, cancellationToken).ConfigureAwait(false),
            SqlContextKind.Column => await GetQueryColumnItemsAsync(context, cancellationToken).ConfigureAwait(false),
            SqlContextKind.MemberAccess => await GetMemberItemsAsync(context, cancellationToken).ConfigureAwait(false),
            _ => [],
        };
    }

    // ---------- By context ----------

    private async Task<IReadOnlyList<CompletionItem>> GetDatabaseItemsAsync(CancellationToken cancellationToken)
        => (await _catalog.GetDatabasesAsync(cancellationToken).ConfigureAwait(false))
            .Select(d => new CompletionItem
            {
                Label = d.Name,
                Kind = CompletionItemKind.Database,
                InsertText = _dialect.QuoteIdentifier(d.Name),
                Detail = d.IsSystem ? "system" : null,
            })
            .ToArray();

    /// <summary>Objects and schemas of the current database, plus the other databases to write database.schema.object.</summary>
    private async Task<IReadOnlyList<CompletionItem>> GetSourceItemsAsync(string? database, SqlSchemaObjectKind[] kinds, CancellationToken cancellationToken)
    {
        var schema = await _catalog.GetDatabaseAsync(database, cancellationToken).ConfigureAwait(false);
        // The schema of a MySQL or SQLite object is its database: it is already listed with the databases.
        IReadOnlyList<CompletionItem> objects = schema is null ? [] : ObjectItems(schema, null, kinds, withSchemas: !_dialect.SchemaIsDatabase);
        if (!_dialect.QueriesOtherDatabases)
            return objects;

        return [.. objects, .. await GetDatabaseItemsAsync(cancellationToken).ConfigureAwait(false)];
    }

    /// <summary>Columns of all the tables named in the batch (SELECT, WHERE, ON…), with their source table.</summary>
    private async Task<IReadOnlyList<CompletionItem>> GetQueryColumnItemsAsync(SqlContext context, CancellationToken cancellationToken)
    {
        List<CompletionItem> items = [];
        foreach (var table in context.Tables)
        {
            if (await _catalog.FindAsync(context, table, cancellationToken).ConfigureAwait(false) is { } resolved)
                items.AddRange(ColumnItems(resolved, table.ExposedName));
        }

        return items;
    }

    /// <summary>
    /// After a dot: alias or table → columns, schema → objects, database → schemas,
    /// database.schema → objects, [database.]schema.table → columns.
    /// </summary>
    private async Task<IReadOnlyList<CompletionItem>> GetMemberItemsAsync(SqlContext context, CancellationToken cancellationToken)
    {
        var qualifier = context.Qualifier;
        var kinds = context.MemberParentKind == SqlContextKind.Procedure ? ProcedureKinds : DataSourceKinds;

        switch (qualifier.Count)
        {
            case 1:
                {
                    var name = qualifier[0];
                    var nameIsQuoted = context.QualifierQuoted[0];
                    if (context.FindTable(name, _dialect, nameIsQuoted) is { } table && await _catalog.FindAsync(context, table, cancellationToken).ConfigureAwait(false) is { } aliased)
                        return ColumnItems(aliased, null);

                    var current = await _catalog.GetDatabaseAsync(context.Database, cancellationToken).ConfigureAwait(false);
                    if (current is not null && _catalog.HasSchema(current, name, nameIsQuoted))
                        return ObjectItems(current, name, kinds, withSchemas: false, schemaIsQuoted: nameIsQuoted);

                    // Table missing from the batch: SELECT Users.| stays valid if Users is in the FROM of another statement.
                    if (current is not null && _catalog.Find(current, null, name, nameIsQuoted) is { Columns.Count: > 0 } direct)
                        return ColumnItems(direct, null);

                    if (!await _catalog.IsDatabaseAsync(name, cancellationToken, nameIsQuoted).ConfigureAwait(false)
                        || await _catalog.GetDatabaseAsync(name, cancellationToken).ConfigureAwait(false) is not { } other)
                        return [];

                    // database.|: its schemas, or directly its objects when the database stands for the schema.
                    return _dialect.SchemaIsDatabase ? ObjectItems(other, null, kinds, withSchemas: false) : SchemaItems(other);
                }

            case 2:
                {
                    if (_dialect.SchemaIsDatabase
                        && await _catalog.IsDatabaseAsync(qualifier[0], cancellationToken, context.QualifierQuoted[0]).ConfigureAwait(false)
                        && await _catalog.GetDatabaseAsync(qualifier[0], cancellationToken).ConfigureAwait(false) is { } remote
                        && _catalog.Find(remote, null, qualifier[1], context.QualifierQuoted[1]) is { } remoteTable)
                        return ColumnItems(remoteTable, null);

                    var current = await _catalog.GetDatabaseAsync(context.Database, cancellationToken).ConfigureAwait(false);
                    if (current is not null
                        && _catalog.Find(current, qualifier[0], qualifier[1], context.QualifierQuoted[1], context.QualifierQuoted[0]) is { } table)
                        return ColumnItems(table, null);

                    return await _catalog.IsDatabaseAsync(qualifier[0], cancellationToken, context.QualifierQuoted[0]).ConfigureAwait(false)
                        && await _catalog.GetDatabaseAsync(qualifier[0], cancellationToken).ConfigureAwait(false) is { } other
                        ? ObjectItems(other, qualifier[1], kinds, withSchemas: false, schemaIsQuoted: context.QualifierQuoted[1])
                        : [];
                }

            case 3:
                {
                    var other = await _catalog.GetDatabaseAsync(qualifier[0], cancellationToken).ConfigureAwait(false);
                    return other is not null
                        && _catalog.Find(other, qualifier[1], qualifier[2], context.QualifierQuoted[2], context.QualifierQuoted[1]) is { } table
                        ? ColumnItems(table, null)
                        : [];
                }

            default:
                return [];
        }
    }

    // ---------- Building suggestions ----------

    /// <summary>Objects of a database, of a schema if specified; the schemas themselves to continue with "schema.".</summary>
    private IReadOnlyList<CompletionItem> ObjectItems(SqlDatabaseSchema database, string? schema, SqlSchemaObjectKind[] kinds, bool withSchemas, bool schemaIsQuoted = false)
    {
        var objects = database.Objects
            .Where(o => kinds.Contains(o.Kind))
            .Where(o => schema is null || _dialect.IdentifierEquals(o.Schema, leftQuoted: true, schema, schemaIsQuoted))
            .Select(o => new CompletionItem
            {
                Label = o.Name,
                Kind = ToItemKind(o.Kind),
                // Outside the default schema, the schema is inserted: "Users" alone would target dbo.Users.
                InsertText = schema is not null || IsDefaultSchema(database, o.Schema)
                    ? _dialect.QuoteIdentifier(o.Name)
                    : $"{_dialect.QuoteIdentifier(o.Schema)}.{_dialect.QuoteIdentifier(o.Name)}",
                Detail = o.Schema,
            });

        return withSchemas ? [.. objects, .. SchemaItems(database)] : [.. objects];
    }

    /// <summary>Schema of unqualified names: the dialect's one (dbo, public), or the database itself (MySQL, SQLite).</summary>
    private bool IsDefaultSchema(SqlDatabaseSchema database, string schema)
        => (_dialect.DefaultSchema is { } defaultSchema
                && _dialect.IdentifierEquals(schema, leftQuoted: true, defaultSchema, rightQuoted: false))
            || (_dialect.SchemaIsDatabase && _dialect.IdentifierEquals(schema, leftQuoted: true, database.Name, rightQuoted: true));

    private IReadOnlyList<CompletionItem> ColumnItems(SqlSchemaObject table, string? owner)
        => table.Columns.Select(c => new CompletionItem
        {
            Label = c.Name,
            Kind = CompletionItemKind.Column,
            InsertText = _dialect.QuoteIdentifier(c.Name),
            // Several tables in the batch: tell where the column comes from.
            Detail = owner is null ? c.DataType : $"{c.DataType} · {owner}",
        }).ToArray();

    private IReadOnlyList<CompletionItem> SchemaItems(SqlDatabaseSchema database)
        => database.Objects.Select(o => o.Schema)
            .Distinct(_dialect.IdentifierCasing == SqlIdentifierCasing.Insensitive ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal)
            .Order(_dialect.IdentifierCasing == SqlIdentifierCasing.Insensitive ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal)
            .Select(s => new CompletionItem
        {
            Label = s,
            Kind = CompletionItemKind.Schema,
            InsertText = _dialect.QuoteIdentifier(s),
            Detail = database.Name,
        }).ToArray();

    private static CompletionItemKind ToItemKind(SqlSchemaObjectKind kind) => kind switch
    {
        SqlSchemaObjectKind.Table => CompletionItemKind.Table,
        SqlSchemaObjectKind.View => CompletionItemKind.View,
        _ => CompletionItemKind.Procedure,
    };
}
