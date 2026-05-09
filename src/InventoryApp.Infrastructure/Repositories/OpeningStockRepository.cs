using Dapper;
using InventoryApp.Core.DTOs;
using InventoryApp.Core.Interfaces;
using InventoryApp.Infrastructure.Persistence;

namespace InventoryApp.Infrastructure.Repositories;

public sealed class OpeningStockRepository : IOpeningStockRepository
{
    private readonly SqliteConnectionFactory _connectionFactory;

    public OpeningStockRepository(SqliteConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<OpeningStockDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        const string sql = @"
SELECT os.id AS Id, os.product_id AS ProductId, p.code AS ProductCode,
       os.qty AS Qty, os.created_at AS CreatedAt, os.note AS Note
FROM opening_stock os
JOIN products p ON p.id = os.product_id
ORDER BY os.id DESC;";

        await using var conn = _connectionFactory.CreateConnection();
        await conn.OpenAsync(cancellationToken);
        var rows = await conn.QueryAsync<OpeningStockDto>(sql);
        return rows.ToList();
    }

    public async Task<long> InsertAsync(CreateOpeningStockRequest request, DateTime createdAtUtc, CancellationToken cancellationToken = default)
    {
        const string sql = @"
INSERT INTO opening_stock(product_id, qty, created_at, note)
VALUES(@ProductId, @Qty, @CreatedAt, @Note);
SELECT last_insert_rowid();";

        await using var conn = _connectionFactory.CreateConnection();
        await conn.OpenAsync(cancellationToken);
        return await conn.ExecuteScalarAsync<long>(sql, new
        {
            request.ProductId,
            request.Qty,
            CreatedAt = createdAtUtc.ToString("O"),
            request.Note
        });
    }
}
