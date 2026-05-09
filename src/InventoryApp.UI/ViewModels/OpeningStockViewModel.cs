using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InventoryApp.Core.DTOs;
using InventoryApp.Core.Interfaces;

namespace InventoryApp.UI.ViewModels;

public partial class OpeningStockViewModel : ViewModelBase
{
    private readonly IOpeningStockService _openingStockService;
    private readonly IProductService _productService;
    private readonly Action _goBack;

    public OpeningStockViewModel(IOpeningStockService openingStockService, IProductService productService, Action goBack)
    {
        _openingStockService = openingStockService;
        _productService = productService;
        _goBack = goBack;
    }

    public ObservableCollection<ProductDto> Products { get; } = new();
    public ObservableCollection<OpeningStockDto> Rows { get; } = new();

    [ObservableProperty] private ProductDto? _selectedProduct;
    [ObservableProperty] private decimal _qty = 1;
    [ObservableProperty] private string _note = string.Empty;
    [ObservableProperty] private string _message = "";

    [RelayCommand]
    private async Task LoadProductsAsync()
    {
        var products = await _productService.SearchAsync(null);
        Products.Clear();
        foreach (var p in products) Products.Add(p);
    }

    [RelayCommand]
    private async Task LoadRowsAsync()
    {
        var rows = await _openingStockService.GetAllAsync();
        Rows.Clear();
        foreach (var row in rows) Rows.Add(row);
        Message = $"Đã tải {Rows.Count} dòng tồn đầu kỳ.";
    }

    [RelayCommand]
    private async Task AddAsync()
    {
        if (SelectedProduct is null || Qty <= 0)
        {
            Message = "Vui lòng chọn sản phẩm và số lượng hợp lệ.";
            return;
        }

        await _openingStockService.AddAsync(new CreateOpeningStockRequest
        {
            ProductId = SelectedProduct.Id,
            Qty = Qty,
            Note = string.IsNullOrWhiteSpace(Note) ? null : Note.Trim()
        });

        await LoadRowsAsync();
        Message = "Đã thêm tồn đầu kỳ.";
    }

    [RelayCommand]
    private void Back() => _goBack();
}
