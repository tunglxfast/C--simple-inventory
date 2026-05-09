namespace InventoryApp.Core.DTOs;

public sealed class PersonSummaryRowDto
{
    public string PersonName { get; set; } = string.Empty;
    public decimal ExportQty { get; set; }
    public int DocumentCount { get; set; }
}
