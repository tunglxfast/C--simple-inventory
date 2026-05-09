using InventoryApp.Core.Entities;

namespace InventoryApp.Core.Interfaces;

public interface IStockDocumentService
{
    Task<StockDocument> CreateDraftAsync(StockDocument document, CancellationToken cancellationToken = default);
    Task AddItemAsync(long documentId, StockDocumentItem item, CancellationToken cancellationToken = default);
    Task ConfirmAsync(long documentId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StockDocumentItem>> GetHoldingItemsAsync(CancellationToken cancellationToken = default);
    Task MarkHoldItemAsSoldAsync(long itemId, CancellationToken cancellationToken = default);
    Task MarkHoldItemAsReturnedAsync(long itemId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StockDocument>> GetRecentConfirmedDocumentsAsync(CancellationToken cancellationToken = default);
    Task<long> CancelWithReversalAsync(long documentId, string? reason, CancellationToken cancellationToken = default);
}
