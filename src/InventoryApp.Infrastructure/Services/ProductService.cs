using InventoryApp.Core.DTOs;
using InventoryApp.Core.Entities;
using InventoryApp.Core.Interfaces;

namespace InventoryApp.Infrastructure.Services;

public sealed class ProductService : IProductService
{
    private readonly IProductRepository _repository;
    private readonly IClock _clock;

    public ProductService(IProductRepository repository, IClock clock)
    {
        _repository = repository;
        _clock = clock;
    }

    public async Task<IReadOnlyList<ProductDto>> SearchAsync(string? keyword, CancellationToken cancellationToken = default)
    {
        var items = await _repository.GetAllAsync(keyword, cancellationToken);
        return items.Select(ToDto).ToList();
    }

    public async Task<ProductDto> CreateAsync(CreateProductRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Code)) throw new InvalidOperationException("Mã sản phẩm không được để trống.");
        if (string.IsNullOrWhiteSpace(request.Name)) throw new InvalidOperationException("Tên sản phẩm không được để trống.");
        if (string.IsNullOrWhiteSpace(request.Unit)) throw new InvalidOperationException("Đơn vị tính không được để trống.");

        var exists = await _repository.GetByCodeAsync(request.Code.Trim(), cancellationToken);
        if (exists is not null) throw new InvalidOperationException("Mã sản phẩm đã tồn tại.");

        var now = _clock.UtcNow;
        var entity = new Product
        {
            Code = request.Code.Trim(),
            Name = request.Name.Trim(),
            Unit = request.Unit.Trim(),
            Size = request.Size,
            Category = request.Category,
            Barcode = request.Barcode,
            Color = request.Color,
            Note = request.Note,
            CreatedAt = now,
            UpdatedAt = now
        };

        entity.Id = await _repository.InsertAsync(entity, cancellationToken);
        return ToDto(entity);
    }

    public async Task UpdateAsync(UpdateProductRequest request, CancellationToken cancellationToken = default)
    {
        var existing = await _repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new InvalidOperationException("Không tìm thấy sản phẩm.");

        existing.Name = request.Name.Trim();
        existing.Unit = request.Unit.Trim();
        existing.Size = request.Size;
        existing.Category = request.Category;
        existing.Barcode = request.Barcode;
        existing.Color = request.Color;
        existing.Note = request.Note;
        existing.UpdatedAt = _clock.UtcNow;

        await _repository.UpdateAsync(existing, cancellationToken);
    }

    public async Task DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        var inUse = await _repository.HasTransactionsAsync(id, cancellationToken);
        if (inUse) throw new InvalidOperationException("Không thể xóa sản phẩm đã phát sinh giao dịch.");
        await _repository.DeleteAsync(id, cancellationToken);
    }

    public async Task ChangeCodeAsync(long id, string newCode, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(newCode)) throw new InvalidOperationException("Mã sản phẩm mới không hợp lệ.");
        var existing = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("Không tìm thấy sản phẩm.");

        var codeTaken = await _repository.GetByCodeAsync(newCode.Trim(), cancellationToken);
        if (codeTaken is not null && codeTaken.Id != id)
            throw new InvalidOperationException("Mã sản phẩm mới đã tồn tại.");

        await _repository.UpdateCodeAcrossSystemAsync(id, newCode.Trim(), _clock.UtcNow, cancellationToken);
    }

    private static ProductDto ToDto(Product p) => new()
    {
        Id = p.Id,
        Code = p.Code,
        Name = p.Name,
        Size = p.Size,
        Category = p.Category,
        Unit = p.Unit,
        Barcode = p.Barcode,
        Color = p.Color,
        Note = p.Note
    };
}
