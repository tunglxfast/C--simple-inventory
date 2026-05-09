namespace InventoryApp.Core.Interfaces;

public interface IDatabaseBootstrapper
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
}
