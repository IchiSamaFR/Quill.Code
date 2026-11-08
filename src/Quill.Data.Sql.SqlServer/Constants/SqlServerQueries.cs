namespace Quill.Data.Sql.SqlServer.Constants;

/// <summary>Catalog queries on the system views (sys.*).</summary>
internal static class SqlServerQueries
{
    internal const string Databases = """
        SELECT name, CAST(CASE WHEN database_id <= 4 THEN 1 ELSE 0 END AS bit)
        FROM sys.databases
        WHERE state = 0
        ORDER BY name;
        """;

    // sys.objects.type is a char(2): 'U' matches 'U ' (trailing spaces ignored).
    internal const string Objects = """
        SELECT s.name, o.name,
               CONVERT(nvarchar(max), (SELECT ep.value FROM sys.extended_properties ep
                   WHERE ep.class = 1 AND ep.major_id = o.object_id AND ep.minor_id = 0 AND ep.name = N'MS_Description'))
        FROM sys.objects o
        INNER JOIN sys.schemas s ON s.schema_id = o.schema_id
        WHERE o.type = @type AND o.is_ms_shipped = 0
        ORDER BY s.name, o.name;
        """;

    // user_type_id: an alias type (CREATE TYPE) is displayed under its name, like in SSMS.
    internal const string Columns = """
        SELECT c.name, t.name, c.max_length, c.precision, c.scale,
               c.is_nullable, c.is_identity, c.is_computed,
               CAST(ISNULL((
                   SELECT TOP (1) ic.key_ordinal FROM sys.index_columns ic
                   INNER JOIN sys.indexes i ON i.object_id = ic.object_id AND i.index_id = ic.index_id
                   WHERE i.is_primary_key = 1 AND ic.object_id = c.object_id AND ic.column_id = c.column_id), 0) AS int),
               CAST(CASE WHEN EXISTS (
                   SELECT 1 FROM sys.foreign_key_columns fk
                   WHERE fk.parent_object_id = c.object_id AND fk.parent_column_id = c.column_id)
                   THEN 1 ELSE 0 END AS bit),
               CONVERT(nvarchar(max), (SELECT ep.value FROM sys.extended_properties ep
                   WHERE ep.class = 1 AND ep.major_id = c.object_id AND ep.minor_id = c.column_id AND ep.name = N'MS_Description'))
        FROM sys.columns c
        INNER JOIN sys.types t ON t.user_type_id = c.user_type_id
        WHERE c.object_id = OBJECT_ID(QUOTENAME(@schema) + N'.' + QUOTENAME(@name))
        ORDER BY c.column_id;
        """;

    // Three result sets: current database, objects, columns (linked by object_id).
    internal const string Snapshot = """
        SELECT DB_NAME();

        SELECT o.object_id, s.name, o.name, RTRIM(o.type)
        FROM sys.objects o
        INNER JOIN sys.schemas s ON s.schema_id = o.schema_id
        WHERE o.type IN ('U', 'V', 'P') AND o.is_ms_shipped = 0;

        SELECT c.object_id, c.name, t.name, c.max_length, c.precision, c.scale,
               CASE WHEN t.is_user_defined = 1 AND t.is_assembly_type = 0 THEN TYPE_NAME(t.system_type_id) ELSE t.name END,
               c.is_nullable,
               CAST(ISNULL(pk.key_ordinal, 0) AS int)
        FROM sys.columns c
        INNER JOIN sys.types t ON t.user_type_id = c.user_type_id
        INNER JOIN sys.objects o ON o.object_id = c.object_id
        LEFT JOIN (
            SELECT ic.object_id, ic.column_id, ic.key_ordinal
            FROM sys.index_columns ic
            INNER JOIN sys.indexes i ON i.object_id = ic.object_id AND i.index_id = ic.index_id
            WHERE i.is_primary_key = 1
        ) pk ON pk.object_id = c.object_id AND pk.column_id = c.column_id
        WHERE o.type IN ('U', 'V') AND o.is_ms_shipped = 0
        ORDER BY c.object_id, c.column_id;
        """;
}
