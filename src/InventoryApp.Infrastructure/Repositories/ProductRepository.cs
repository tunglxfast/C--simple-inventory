using Dapper;
using InventoryApp.Core.Entities;
using InventoryApp.Core.Interfaces;
using InventoryApp.Infrastructure.Persistence;

namespace InventoryApp.Infrastructure.Repositories;

public sealed class ProductRepository : IProductRepository
{
    private readonly SqliteConnectionFactory _connectionFactory;

    public ProductRepository(SqliteConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<Product>> GetAllAsync(string? keyword, CancellationToken cancellationToken = default)
    {
        var sql = @"
SELECT id AS Id, code AS Code, name AS Name, size AS Size, category AS Category,
       unit AS Unit, barcode AS Barcode, color AS Color, note AS Note,
       created_at AS CreatedAt, updated_at AS UpdatedAt
FROM products
WHERE (@keyword IS NULL OR lower(code) LIKE @kw OR lower(name) LIKE @kw)
ORDER BY code;";

        var kw = string.IsNullOrWhiteSpace(keyword) ? null : $"%{keyword.Trim().ToLowerInvariant()}%";
        await using var conn = _connectionFactory.CreateConnection();
        await conn.OpenAsync(cancellationToken);
        var rows = await conn.QueryAsync<Product>(sql, new { keyword = kw, kw });
        return rows.ToList();
    }

    public async Task<Product?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        const string sql = @"
SELECT id AS Id, code AS Code, name AS Name, size AS Size, category AS Category,
       unit AS Unit, barcode AS Barcode, color AS Color, note AS Note,
       created_at AS CreatedAt, updated_at AS UpdatedAt
FROM products WHERE id = @id LIMIT 1;";

        await using var conn = _connectionFactory.CreateConnection();
        await conn.OpenAsync(cancellationToken);
        return await conn.QueryFirstOrDefaultAsync<Product>(sql, new { id });
    }

    public async Task<Product?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        const string sql = @"
SELECT id AS Id, code AS Code, name AS Name, size AS Size, category AS Category,
       unit AS Unit, barcode AS Barcode, color AS Color, note AS Note,
       created_at AS CreatedAt, updated_at AS UpdatedAt
FROM products WHERE lower(code) = lower(@code) LIMIT 1;";

        await using var conn = _connectionFactory.CreateConnection();
        await conn.OpenAsync(cancellationToken);
        return await conn.QueryFirstOrDefaultAsync<Product>(sql, new { code });
    }

    public async Task<long> InsertAsync(Product product, CancellationToken cancellationToken = default)
    {
        const string sql = @"
INSERT INTO products(code,name,size,category,unit,barcode,color,note,created_at,updated_at)
VALUES(@Code,@Name,@Size,@Category,@Unit,@Barcode,@Color,@Note,@CreatedAt,@UpdatedAt);
SELECT last_insert_rowid();";

        await using var conn = _connectionFactory.CreateConnection();
        await conn.OpenAsync(cancellationToken);
        return await conn.ExecuteScalarAsync<long>(sql, product);
    }

    public async Task UpdateAsync(Product product, CancellationToken cancellationToken = default)
    {
        const string sql = @"
UPDATE products
SET name=@Name, size=@Size, category=@Category, unit=@Unit, barcode=@Barcode,
    color=@Color, note=@Note, updated_at=@UpdatedAt
WHERE id=@Id;";

        await using var conn = _connectionFactory.CreateConnection();
        await conn.OpenAsync(cancellationToken);
        await conn.ExecuteAsync(sql, product);
    }

    public async Task DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        const string sql = "DELETE FROM products WHERE id=@id;";
        await using var conn = _connectionFactory.CreateConnection();
        await conn.OpenAsync(cancellationToken);
        await conn.ExecuteAsync(sql, new { id });
    }

    public async Task<bool> HasTransactionsAsync(long productId, CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT COUNT(1) FROM stock_document_items WHERE product_id=@productId;";
        await using var conn = _connectionFactory.CreateConnection();
        await conn.OpenAsync(cancellationToken);
        var count = await conn.ExecuteScalarAsync<long>(sql, new { productId });
        return count > 0;
    }

    public async Task UpdateCodeAcrossSystemAsync(long productId, string newCode, DateTime updatedAtUtc, CancellationToken cancellationToken = default)
    {
        const string sql = @"
UPDATE products SET code=@newCode, updated_at=@updatedAt WHERE id=@productId;";

        await using var conn = _connectionFactory.CreateConnection();
        await conn.OpenAsync(cancellationToken);
        await using var tx = await conn.BeginTransactionAsync(cancellationToken);
        await conn.ExecuteAsync(sql, new { productId, newCode, updatedAt = updatedAtUtc }, tx);
        await tx.CommitAsync(cancellationToken);
    }
}
