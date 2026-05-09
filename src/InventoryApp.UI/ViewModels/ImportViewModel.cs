using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InventoryApp.Core.Interfaces;

namespace InventoryApp.UI.ViewModels;

public partial class ImportViewModel : ViewModelBase
{
    private readonly IImportService _importService;
    private readonly Action _goBack;

    public ImportViewModel(IImportService importService, Action goBack)
    {
        _importService = importService;
        _goBack = goBack;
    }

    [ObservableProperty] private string _productsFilePath = string.Empty;
    [ObservableProperty] private string _openingStockFilePath = string.Empty;
    [ObservableProperty] private string _transactionsFilePath = string.Empty;
    [ObservableProperty] private string _message = "Nhập đường dẫn file Excel để import.";

    [RelayCommand]
    private async Task ImportProductsAsync()
    {
        if (string.IsNullOrWhiteSpace(ProductsFilePath))
        {
            Message = "Vui lòng nhập đường dẫn file sản phẩm.";
            return;
        }

        var result = await _importService.ImportProductsAsync(ProductsFilePath.Trim());
        Message = Summarize("Products", result);
    }

    [RelayCommand]
    private async Task ImportOpeningStockAsync()
    {
        if (string.IsNullOrWhiteSpace(OpeningStockFilePath))
        {
            Message = "Vui lòng nhập đường dẫn file tồn đầu kỳ.";
            return;
        }

        var result = await _importService.ImportOpeningStockAsync(OpeningStockFilePath.Trim());
        Message = Summarize("OpeningStock", result);
    }

    [RelayCommand]
    private async Task ImportTransactionsAsync()
    {
        if (string.IsNullOrWhiteSpace(TransactionsFilePath))
        {
            Message = "Vui lòng nhập đường dẫn file transactions.";
            return;
        }

        var result = await _importService.ImportTransactionsAsync(TransactionsFilePath.Trim());
        Message = Summarize("Transactions", result);
    }

    [RelayCommand]
    private void Back() => _goBack();

    private static string Summarize(string label, Core.DTOs.ImportResult result)
    {
        var text = $"{label}: total={result.TotalRows}, success={result.SuccessRows}, failed={result.FailedRows}";
        if (result.Errors.Count > 0)
            text += $" | first error: {result.Errors[0]}";
        return text;
    }
}
