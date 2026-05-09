using InventoryApp.Core.DTOs;

namespace InventoryApp.Core.Interfaces;

public interface IImportService
{
    Task<ImportResult> ImportProductsAsync(string filePath, CancellationToken cancellationToken = default);
    Task<ImportResult> ImportOpeningStockAsync(string filePath, CancellationToken cancellationToken = default);
    Task<ImportResult> ImportTransactionsAsync(string filePath, CancellationToken cancellationToken = default);
}
