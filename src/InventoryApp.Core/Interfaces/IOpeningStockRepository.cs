using InventoryApp.Core.DTOs;

namespace InventoryApp.Core.Interfaces;

public interface IOpeningStockRepository
{
    Task<IReadOnlyList<OpeningStockDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<long> InsertAsync(CreateOpeningStockRequest request, DateTime createdAtUtc, CancellationToken cancellationToken = default);
}
