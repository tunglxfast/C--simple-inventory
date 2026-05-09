using InventoryApp.Core.Interfaces;

namespace InventoryApp.UI.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    public MainViewModel(
        IProductService productService,
        IStockDocumentService stockDocumentService,
        IOpeningStockService openingStockService,
        IImportService importService,
        IReportService reportService,
        IExportService exportService)
    {
        LoginViewModel = new LoginViewModel(SwitchToDashboard);
        DashboardViewModel = new DashboardViewModel(
            SwitchToProducts,
            SwitchToStockOut,
            SwitchToStockIn,
            SwitchToOpeningStock,
            SwitchToImport,
            SwitchToReport);
        ProductViewModel = new ProductViewModel(productService, SwitchToDashboard);
        StockOutViewModel = new StockOutViewModel(stockDocumentService, productService, SwitchToDashboard);
        StockInViewModel = new StockInViewModel(stockDocumentService, productService, SwitchToDashboard);
        OpeningStockViewModel = new OpeningStockViewModel(openingStockService, productService, SwitchToDashboard);
        ImportViewModel = new ImportViewModel(importService, SwitchToDashboard);
        ReportViewModel = new ReportViewModel(reportService, exportService, SwitchToDashboard);
        CurrentViewModel = LoginViewModel;
    }

    public string Title => "InventoryApp v0.1";

    public LoginViewModel LoginViewModel { get; }
    public DashboardViewModel DashboardViewModel { get; }
    public ProductViewModel ProductViewModel { get; }
    public StockOutViewModel StockOutViewModel { get; }
    public StockInViewModel StockInViewModel { get; }
    public OpeningStockViewModel OpeningStockViewModel { get; }
    public ImportViewModel ImportViewModel { get; }
    public ReportViewModel ReportViewModel { get; }

    private ViewModelBase _currentViewModel = default!;
    public ViewModelBase CurrentViewModel
    {
        get => _currentViewModel;
        set => SetProperty(ref _currentViewModel, value);
    }

    private void SwitchToDashboard() => CurrentViewModel = DashboardViewModel;

    private async void SwitchToProducts()
    {
        CurrentViewModel = ProductViewModel;
        await ProductViewModel.LoadCommand.ExecuteAsync(null);
    }

    private async void SwitchToStockOut()
    {
        CurrentViewModel = StockOutViewModel;
        await StockOutViewModel.LoadProductsCommand.ExecuteAsync(null);
        await StockOutViewModel.LoadHoldingCommand.ExecuteAsync(null);
        await StockOutViewModel.LoadConfirmedDocsCommand.ExecuteAsync(null);
    }

    private async void SwitchToStockIn()
    {
        CurrentViewModel = StockInViewModel;
        await StockInViewModel.LoadProductsCommand.ExecuteAsync(null);
    }

    private async void SwitchToOpeningStock()
    {
        CurrentViewModel = OpeningStockViewModel;
        await OpeningStockViewModel.LoadProductsCommand.ExecuteAsync(null);
        await OpeningStockViewModel.LoadRowsCommand.ExecuteAsync(null);
    }

    private void SwitchToImport() => CurrentViewModel = ImportViewModel;

    private async void SwitchToReport()
    {
        CurrentViewModel = ReportViewModel;
        await ReportViewModel.RunReportCommand.ExecuteAsync(null);
    }
}
