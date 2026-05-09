using InventoryApp.Core.Entities;

namespace InventoryApp.Core.Interfaces;

public interface IProductRepository
{
    Task<IReadOnlyList<Product>> GetAllAsync(string? keyword, CancellationToken cancellationToken = default);
    Task<Product?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<Product?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<long> InsertAsync(Product product, CancellationToken cancellationToken = default);
    Task UpdateAsync(Product product, CancellationToken cancellationToken = default);
    Task DeleteAsync(long id, CancellationToken cancellationToken = default);
    Task<bool> HasTransactionsAsync(long productId, CancellationToken cancellationToken = default);
    Task UpdateCodeAcrossSystemAsync(long productId, string newCode, DateTime updatedAtUtc, CancellationToken cancellationToken = default);
}
