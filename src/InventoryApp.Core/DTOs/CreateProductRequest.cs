namespace InventoryApp.Core.DTOs;

public sealed class CreateProductRequest
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public string? Size { get; set; }
    public string? Category { get; set; }
    public string? Barcode { get; set; }
    public string? Color { get; set; }
    public string? Note { get; set; }
}
