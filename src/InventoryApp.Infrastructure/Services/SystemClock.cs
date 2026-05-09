using InventoryApp.Core.Interfaces;

namespace InventoryApp.Infrastructure.Services;

public sealed class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}
