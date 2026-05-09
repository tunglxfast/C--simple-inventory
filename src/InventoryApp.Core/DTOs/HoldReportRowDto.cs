namespace InventoryApp.Core.DTOs;

public sealed class HoldReportRowDto
{
    public string DocNo { get; set; } = string.Empty;
    public long ItemId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public decimal Qty { get; set; }
    public string ItemStatus { get; set; } = string.Empty;
    public DateTime DocDate { get; set; }
}
