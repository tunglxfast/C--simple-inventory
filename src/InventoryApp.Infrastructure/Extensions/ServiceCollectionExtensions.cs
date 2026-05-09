using InventoryApp.Core.Interfaces;
using InventoryApp.Infrastructure.Configuration;
using InventoryApp.Infrastructure.Persistence;
using InventoryApp.Infrastructure.Repositories;
using InventoryApp.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace InventoryApp.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInventoryInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<AppSettings>(configuration.GetSection(AppSettings.SectionName));
        services.AddSingleton<SqliteConnectionFactory>();
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IDatabaseBootstrapper, SqliteDatabaseBootstrapper>();
        services.AddSingleton<IProductRepository, ProductRepository>();
        services.AddSingleton<IProductService, ProductService>();
        services.AddSingleton<IOpeningStockRepository, OpeningStockRepository>();
        services.AddSingleton<IOpeningStockService, OpeningStockService>();
        services.AddSingleton<IImportService, ImportService>();
        services.AddSingleton<IReportService, ReportService>();
        services.AddSingleton<IExportService, ExportService>();
        services.AddSingleton<IDocNoGenerator, DocNoGenerator>();
        services.AddSingleton<IStockDocumentService, StockDocumentService>();
        return services;
    }
}
