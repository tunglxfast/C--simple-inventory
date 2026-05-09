using InventoryApp.Core.Enums;

namespace InventoryApp.Core.Entities;

public sealed class StockDocument
{
    public long Id { get; set; }
    public string DocNo { get; set; } = string.Empty;
    public DocumentType DocType { get; set; }
    public string? ReferenceDocNo { get; set; }
    public long? ReversedDocumentId { get; set; }
    public string? CustomerName { get; set; }
    public string? SaleEmployeeName { get; set; }
    public string? RequestEmployeeName { get; set; }
    public string? Area { get; set; }
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public PaymentMethod? PaymentMethod { get; set; }
    public DocumentStatus DocumentStatus { get; set; }
    public string? Note { get; set; }
    public long? ReportingPeriodId { get; set; }
    public DateTime DocDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
