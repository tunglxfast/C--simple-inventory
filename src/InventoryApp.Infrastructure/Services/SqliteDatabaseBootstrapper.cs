using InventoryApp.Core.Interfaces;
using InventoryApp.Infrastructure.Persistence;

namespace InventoryApp.Infrastructure.Services;

public sealed class SqliteDatabaseBootstrapper : IDatabaseBootstrapper
{
    private readonly SqliteConnectionFactory _connectionFactory;

    public SqliteDatabaseBootstrapper(SqliteConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        var scriptPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "Scripts", "001_init.sql"));
        if (!File.Exists(scriptPath))
        {
            var fallback = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "src", "InventoryApp.Database", "Scripts", "001_init.sql"));
            scriptPath = fallback;
        }

        var sql = await File.ReadAllTextAsync(scriptPath, cancellationToken);

        await using var conn = _connectionFactory.CreateConnection();
        await conn.OpenAsync(cancellationToken);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }
}
