namespace InventoryApp.Core.DTOs;

public sealed class CreateOpeningStockRequest
{
    public long ProductId { get; set; }
    public decimal Qty { get; set; }
    public string? Note { get; set; }
}
