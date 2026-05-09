using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using InventoryApp.Core.Interfaces;
using InventoryApp.Infrastructure.Extensions;
using InventoryApp.UI.ViewModels;
using InventoryApp.UI.Views;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace InventoryApp.UI;

public partial class App : Application
{
    public IServiceProvider Services { get; private set; } = default!;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override async void OnFrameworkInitializationCompleted()
    {
        try
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: true)
                .Build();

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddLogging(builder => builder.AddDebug());
            serviceCollection.AddSingleton<IConfiguration>(configuration);
            serviceCollection.AddInventoryInfrastructure(configuration);
            serviceCollection.AddSingleton<MainViewModel>();
            serviceCollection.AddSingleton<MainWindow>();

            Services = serviceCollection.BuildServiceProvider();

            var bootstrapper = Services.GetRequiredService<IDatabaseBootstrapper>();
            await bootstrapper.InitializeAsync();

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = Services.GetRequiredService<MainWindow>();
                desktop.MainWindow.Show();
            }
        }
        catch (Exception ex)
        {
            var logDir = Path.Combine(Directory.GetCurrentDirectory(), "logs");
            Directory.CreateDirectory(logDir);
            var logPath = Path.Combine(logDir, "startup-error.log");
            await File.WriteAllTextAsync(logPath, ex.ToString());
            throw;
        }

        base.OnFrameworkInitializationCompleted();
    }
}
