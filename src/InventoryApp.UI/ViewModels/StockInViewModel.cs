using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InventoryApp.Core.DTOs;
using InventoryApp.Core.Entities;
using InventoryApp.Core.Enums;
using InventoryApp.Core.Interfaces;

namespace InventoryApp.UI.ViewModels;

public partial class StockInViewModel : ViewModelBase
{
    private readonly IStockDocumentService _stockDocumentService;
    private readonly IProductService _productService;
    private readonly Action _goBack;

    public StockInViewModel(IStockDocumentService stockDocumentService, IProductService productService, Action goBack)
    {
        _stockDocumentService = stockDocumentService;
        _productService = productService;
        _goBack = goBack;
        DocDate = DateTime.Today;
    }

    public ObservableCollection<ProductDto> Products { get; } = new();
    public ObservableCollection<StockDocumentItem> Items { get; } = new();

    [ObservableProperty] private string _message = "Tạo phiếu nhập (bản thô).";
    [ObservableProperty] private DateTime _docDate;
    [ObservableProperty] private string _note = string.Empty;
    [ObservableProperty] private long _documentId;
    [ObservableProperty] private string _docNo = string.Empty;
    [ObservableProperty] private ProductDto? _selectedProduct;
    [ObservableProperty] private decimal _qty = 1;

    [RelayCommand]
    private async Task LoadProductsAsync()
    {
        var products = await _productService.SearchAsync(null);
        Products.Clear();
        foreach (var p in products) Products.Add(p);
        Message = $"Đã tải {Products.Count} sản phẩm.";
    }

    [RelayCommand]
    private async Task CreateDraftAsync()
    {
        var doc = new StockDocument
        {
            DocType = DocumentType.Import,
            DocDate = DocDate,
            Note = string.IsNullOrWhiteSpace(Note) ? null : Note.Trim()
        };

        var created = await _stockDocumentService.CreateDraftAsync(doc);
        DocumentId = created.Id;
        DocNo = created.DocNo;
        Items.Clear();
        Message = $"Đã tạo phiếu nhập nháp: {DocNo}";
    }

    [RelayCommand]
    private async Task AddItemAsync()
    {
        if (DocumentId <= 0)
        {
            Message = "Vui lòng tạo phiếu nháp trước.";
            return;
        }

        if (SelectedProduct is null || Qty <= 0)
        {
            Message = "Vui lòng chọn sản phẩm và số lượng hợp lệ.";
            return;
        }

        var item = new StockDocumentItem
        {
            DocumentId = DocumentId,
            ProductId = SelectedProduct.Id,
            Qty = Qty,
            StockEffectType = StockEffectType.Import
        };

        await _stockDocumentService.AddItemAsync(DocumentId, item);
        Items.Add(item);
        Message = $"Đã thêm dòng nhập {SelectedProduct.Code}.";
    }

    [RelayCommand]
    private async Task ConfirmAsync()
    {
        if (DocumentId <= 0)
        {
            Message = "Chưa có phiếu để xác nhận.";
            return;
        }

        await _stockDocumentService.ConfirmAsync(DocumentId);
        Message = $"Đã xác nhận phiếu nhập {DocNo}.";
    }

    [RelayCommand]
    private void Back() => _goBack();
}
