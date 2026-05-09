using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;
using InventoryApp.Infrastructure.Configuration;

namespace InventoryApp.Infrastructure.Persistence;

public sealed class SqliteConnectionFactory
{
    private readonly string _connectionString;

    public SqliteConnectionFactory(IOptions<AppSettings> options)
    {
        var dbPath = options.Value.DatabasePath;
        var fullPath = Path.GetFullPath(dbPath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath) ?? ".");
        _connectionString = $"Data Source={fullPath};Foreign Keys=True";
    }

    public SqliteConnection CreateConnection() => new(_connectionString);
}
