namespace InventoryApp.Core.DTOs;

public sealed class ImportResult
{
    public int TotalRows { get; set; }
    public int SuccessRows { get; set; }
    public int FailedRows => Errors.Count;
    public List<string> Errors { get; } = new();
}
