namespace InventoryApp.Core.Interfaces;

public interface IClock
{
    DateTime UtcNow { get; }
}
