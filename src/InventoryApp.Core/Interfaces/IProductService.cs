using InventoryApp.Core.DTOs;

namespace InventoryApp.Core.Interfaces;

public interface IProductService
{
    Task<IReadOnlyList<ProductDto>> SearchAsync(string? keyword, CancellationToken cancellationToken = default);
    Task<ProductDto> CreateAsync(CreateProductRequest request, CancellationToken cancellationToken = default);
    Task UpdateAsync(UpdateProductRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(long id, CancellationToken cancellationToken = default);
    Task ChangeCodeAsync(long id, string newCode, CancellationToken cancellationToken = default);
}
