using InventoryApp.Core.DTOs;
using InventoryApp.Core.Interfaces;

namespace InventoryApp.Infrastructure.Services;

public sealed class OpeningStockService : IOpeningStockService
{
    private readonly IOpeningStockRepository _repository;
    private readonly IClock _clock;

    public OpeningStockService(IOpeningStockRepository repository, IClock clock)
    {
        _repository = repository;
        _clock = clock;
    }

    public Task<IReadOnlyList<OpeningStockDto>> GetAllAsync(CancellationToken cancellationToken = default)
        => _repository.GetAllAsync(cancellationToken);

    public async Task AddAsync(CreateOpeningStockRequest request, CancellationToken cancellationToken = default)
    {
        if (request.ProductId <= 0) throw new InvalidOperationException("Sản phẩm không hợp lệ.");
        if (request.Qty <= 0) throw new InvalidOperationException("Số lượng đầu kỳ phải > 0.");
        await _repository.InsertAsync(request, _clock.UtcNow, cancellationToken);
    }
}
