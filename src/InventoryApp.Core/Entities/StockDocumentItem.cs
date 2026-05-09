using InventoryApp.Core.Enums;

namespace InventoryApp.Core.Entities;

public sealed class StockDocumentItem
{
    public long Id { get; set; }
    public long DocumentId { get; set; }
    public long ProductId { get; set; }
    public decimal Qty { get; set; }
    public StockEffectType StockEffectType { get; set; }
    public ItemStatus? ItemStatus { get; set; }
    public string? Note { get; set; }
}
