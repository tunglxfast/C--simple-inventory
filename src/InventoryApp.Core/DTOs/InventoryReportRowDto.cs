namespace InventoryApp.Core.DTOs;

public sealed class InventoryReportRowDto
{
    public long ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public decimal OpeningQty { get; set; }
    public decimal ImportQty { get; set; }
    public decimal ExportQty { get; set; }
    public decimal ReturnQty { get; set; }
    public decimal DamageQty { get; set; }
    public decimal ClosingQty { get; set; }
}
