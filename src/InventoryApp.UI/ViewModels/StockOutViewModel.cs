using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InventoryApp.Core.DTOs;
using InventoryApp.Core.Entities;
using InventoryApp.Core.Enums;
using InventoryApp.Core.Interfaces;

namespace InventoryApp.UI.ViewModels;

public partial class StockOutViewModel : ViewModelBase
{
    private readonly IStockDocumentService _stockDocumentService;
    private readonly IProductService _productService;
    private readonly Action _goBack;

    public StockOutViewModel(IStockDocumentService stockDocumentService, IProductService productService, Action goBack)
    {
        _stockDocumentService = stockDocumentService;
        _productService = productService;
        _goBack = goBack;

        DocDate = DateTime.Today;
        SelectedStockEffectType = "EXPORT";
    }

    public ObservableCollection<ProductDto> Products { get; } = new();
    public ObservableCollection<StockDocumentItem> Items { get; } = new();
    public ObservableCollection<StockDocumentItem> HoldingItems { get; } = new();
    public ObservableCollection<StockDocument> ConfirmedDocuments { get; } = new();
    public IReadOnlyList<string> StockEffectTypes { get; } = new[] { "EXPORT", "HOLD", "RETURN", "DAMAGE" };

    [ObservableProperty] private string _message = "Tạo phiếu xuất thử nghiệm (bản thô).";
    [ObservableProperty] private DateTime _docDate;
    [ObservableProperty] private string _customerName = string.Empty;
    [ObservableProperty] private string _note = string.Empty;
    [ObservableProperty] private long _documentId;
    [ObservableProperty] private string _docNo = string.Empty;

    [ObservableProperty] private ProductDto? _selectedProduct;
    [ObservableProperty] private decimal _qty = 1;
    [ObservableProperty] private string _selectedStockEffectType = "EXPORT";
    [ObservableProperty] private StockDocumentItem? _selectedHoldingItem;
    [ObservableProperty] private StockDocument? _selectedConfirmedDocument;
    [ObservableProperty] private string _cancelReason = string.Empty;

    [RelayCommand]
    private async Task LoadProductsAsync()
    {
        var products = await _productService.SearchAsync(null);
        Products.Clear();
        foreach (var p in products) Products.Add(p);
        Message = $"Đã tải {Products.Count} sản phẩm.";
    }

    [RelayCommand]
    private async Task LoadHoldingAsync()
    {
        var items = await _stockDocumentService.GetHoldingItemsAsync();
        HoldingItems.Clear();
        foreach (var item in items) HoldingItems.Add(item);
        Message = $"Đã tải {HoldingItems.Count} dòng HOLD đang mở.";
    }

    [RelayCommand]
    private async Task LoadConfirmedDocsAsync()
    {
        var docs = await _stockDocumentService.GetRecentConfirmedDocumentsAsync();
        ConfirmedDocuments.Clear();
        foreach (var doc in docs) ConfirmedDocuments.Add(doc);
        Message = $"Đã tải {ConfirmedDocuments.Count} phiếu CONFIRMED gần nhất.";
    }

    [RelayCommand]
    private async Task CreateDraftAsync()
    {
        var doc = new StockDocument
        {
            DocType = DocumentType.Export,
            DocDate = DocDate,
            CustomerName = string.IsNullOrWhiteSpace(CustomerName) ? null : CustomerName.Trim(),
            Note = string.IsNullOrWhiteSpace(Note) ? null : Note.Trim()
        };

        var created = await _stockDocumentService.CreateDraftAsync(doc);
        DocumentId = created.Id;
        DocNo = created.DocNo;
        Items.Clear();
        Message = $"Đã tạo phiếu nháp: {DocNo}";
    }

    [RelayCommand]
    private async Task AddItemAsync()
    {
        if (DocumentId <= 0)
        {
            Message = "Vui lòng tạo phiếu nháp trước.";
            return;
        }

        if (SelectedProduct is null)
        {
            Message = "Vui lòng chọn sản phẩm.";
            return;
        }

        if (Qty <= 0)
        {
            Message = "Số lượng phải > 0.";
            return;
        }

        var stockEffectType = Enum.Parse<StockEffectType>(SelectedStockEffectType, true);
        var item = new StockDocumentItem
        {
            DocumentId = DocumentId,
            ProductId = SelectedProduct.Id,
            Qty = Qty,
            StockEffectType = stockEffectType,
            ItemStatus = stockEffectType == StockEffectType.Hold ? ItemStatus.Hold : null
        };

        try
        {
            await _stockDocumentService.AddItemAsync(DocumentId, item);
            Items.Add(item);
            Message = $"Đã thêm dòng {SelectedProduct.Code} - {SelectedStockEffectType}.";
        }
        catch (Exception ex)
        {
            Message = ex.Message;
        }
    }

    [RelayCommand]
    private async Task ConfirmAsync()
    {
        if (DocumentId <= 0)
        {
            Message = "Chưa có phiếu để xác nhận.";
            return;
        }

        try
        {
            await _stockDocumentService.ConfirmAsync(DocumentId);
            Message = $"Đã xác nhận phiếu {DocNo}.";
            await LoadConfirmedDocsAsync();
            await LoadHoldingAsync();
        }
        catch (Exception ex)
        {
            Message = ex.Message;
        }
    }

    [RelayCommand]
    private async Task MarkSoldAsync()
    {
        if (SelectedHoldingItem is null)
        {
            Message = "Vui lòng chọn dòng HOLD.";
            return;
        }

        try
        {
            await _stockDocumentService.MarkHoldItemAsSoldAsync(SelectedHoldingItem.Id);
            await LoadHoldingAsync();
            Message = "Đã chuyển HOLD -> SOLD.";
        }
        catch (Exception ex)
        {
            Message = ex.Message;
        }
    }

    [RelayCommand]
    private async Task MarkReturnedAsync()
    {
        if (SelectedHoldingItem is null)
        {
            Message = "Vui lòng chọn dòng HOLD.";
            return;
        }

        try
        {
            await _stockDocumentService.MarkHoldItemAsReturnedAsync(SelectedHoldingItem.Id);
            await LoadHoldingAsync();
            Message = "Đã chuyển HOLD -> RETURNED.";
        }
        catch (Exception ex)
        {
            Message = ex.Message;
        }
    }

    [RelayCommand]
    private async Task CancelSelectedDocumentAsync()
    {
        if (SelectedConfirmedDocument is null)
        {
            Message = "Vui lòng chọn phiếu CONFIRMED cần hủy.";
            return;
        }

        try
        {
            var reversalId = await _stockDocumentService.CancelWithReversalAsync(
                SelectedConfirmedDocument.Id,
                string.IsNullOrWhiteSpace(CancelReason) ? "User requested cancellation" : CancelReason.Trim());

            Message = $"Đã hủy phiếu {SelectedConfirmedDocument.DocNo} và tạo phiếu đảo ID={reversalId}.";
            await LoadConfirmedDocsAsync();
            await LoadHoldingAsync();
        }
        catch (Exception ex)
        {
            Message = ex.Message;
        }
    }

    [RelayCommand]
    private void Back() => _goBack();
}
