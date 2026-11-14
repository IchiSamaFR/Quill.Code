using System.Collections.Concurrent;
using System.IO;
using Quill.Code.CSharp.Linq;
using Quill.Code.CSharp.Linq.Sqlite;
using Quill.Code.Demo.Constants;
using Quill.Code.Execution;
using Quill.Code.Scripting;
using Quill.Code.Sql.Sqlite;
using Quill.Code.Sql.SqlServer.Schema;
using Quill.Data.Sql;
using Quill.Data.Sql.Sqlite;

namespace Quill.Code.Demo.Databases;

/// <summary>
/// Fake "shop" database: a SQLite file in the temporary folder, recreated and filled at startup. It serves both
/// the editor (structure read by <see cref="SqliteSchemaReader"/>) and SQL and LINQ execution.
/// </summary>
internal sealed class DemoDatabase
{
    public DemoDatabase()
    {
        Folder = Path.Combine(Path.GetTempPath(), DemoConstants.DatabaseFolder);
        ConnectionString = $"Data Source={Path.Combine(Folder, DemoConstants.DatabaseFile)}";
        LinqExecutor = new LinqQueryExecutor(LinqProvider);
    }

    public string Folder { get; }

    public string ConnectionString { get; }

    public SqliteQueryExecutor SqlExecutor { get; } = new();

    /// <summary>Same instance for the editor and execution: same compiled DbContext.</summary>
    public SqliteLinqProvider LinqProvider { get; } = new();

    public LinqQueryExecutor LinqExecutor { get; }

    /// <summary>Structure read after creation; null while it has failed.</summary>
    public SqlDatabaseSchema? Schema { get; private set; }

    /// <summary>SQL editor catalog on this database.</summary>
    public ISqlCatalog? Catalog => Schema is null ? null : new SqlCatalog([Schema]);

    /// <summary>Replays the database script then reads its structure; returns the script errors.</summary>
    public async Task<IReadOnlyList<LogEntry>> CreateAsync(CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(Folder);

        var errors = new ConcurrentQueue<LogEntry>();
        var options = new DataRunOptions
        {
            Log = entry =>
            {
                if (entry.IsError)
                    errors.Enqueue(entry);
            },
        };

        await SqlExecutor.RunAsync(ConnectionString, DemoResources.Read(DemoConstants.DatabaseScript), options, cancellationToken);
        Schema = await new SqliteSchemaReader().GetSchemaAsync(ConnectionString, null, cancellationToken);
        return [.. errors];
    }
}
