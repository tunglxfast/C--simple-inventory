namespace InventoryApp.Core.DTOs;

public sealed class OpeningStockDto
{
    public long Id { get; set; }
    public long ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public decimal Qty { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? Note { get; set; }
}
