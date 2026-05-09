using CommunityToolkit.Mvvm.Input;

namespace InventoryApp.UI.ViewModels;

public partial class DashboardViewModel : ViewModelBase
{
    private readonly Action _openProducts;
    private readonly Action _openStockOut;
    private readonly Action _openStockIn;
    private readonly Action _openOpeningStock;
    private readonly Action _openImport;
    private readonly Action _openReport;

    public DashboardViewModel(
        Action openProducts,
        Action openStockOut,
        Action openStockIn,
        Action openOpeningStock,
        Action openImport,
        Action openReport)
    {
        _openProducts = openProducts;
        _openStockOut = openStockOut;
        _openStockIn = openStockIn;
        _openOpeningStock = openOpeningStock;
        _openImport = openImport;
        _openReport = openReport;
    }

    public string Greeting => "Tổng quan kho";
    public int TotalProducts => 0;
    public int LowStockCount => 0;
    public int HoldingBills => 0;

    [RelayCommand] private void OpenProducts() => _openProducts();
    [RelayCommand] private void OpenStockOut() => _openStockOut();
    [RelayCommand] private void OpenStockIn() => _openStockIn();
    [RelayCommand] private void OpenOpeningStock() => _openOpeningStock();
    [RelayCommand] private void OpenImport() => _openImport();
    [RelayCommand] private void OpenReport() => _openReport();
}
