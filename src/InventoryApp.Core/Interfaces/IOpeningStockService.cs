using InventoryApp.Core.DTOs;

namespace InventoryApp.Core.Interfaces;

public interface IOpeningStockService
{
    Task<IReadOnlyList<OpeningStockDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(CreateOpeningStockRequest request, CancellationToken cancellationToken = default);
}
