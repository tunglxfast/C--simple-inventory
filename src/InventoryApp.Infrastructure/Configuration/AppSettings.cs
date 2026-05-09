namespace InventoryApp.Infrastructure.Configuration;

public sealed class AppSettings
{
    public const string SectionName = "App";

    public string DatabasePath { get; set; } = "inventory.db";
}
