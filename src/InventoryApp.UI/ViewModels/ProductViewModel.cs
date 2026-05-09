using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InventoryApp.Core.DTOs;
using InventoryApp.Core.Interfaces;

namespace InventoryApp.UI.ViewModels;

public partial class ProductViewModel : ViewModelBase
{
    private readonly IProductService _productService;
    private readonly Action _goBack;

    public ProductViewModel(IProductService productService, Action goBack)
    {
        _productService = productService;
        _goBack = goBack;
    }

    public ObservableCollection<ProductDto> Products { get; } = new();

    [ObservableProperty]
    private ProductDto? _selectedProduct;

    [ObservableProperty]
    private string _keyword = string.Empty;

    [ObservableProperty]
    private string _code = string.Empty;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _unit = "Bộ";

    [ObservableProperty]
    private string _size = string.Empty;

    [ObservableProperty]
    private string _category = string.Empty;

    [ObservableProperty]
    private string _message = "";

    [ObservableProperty]
    private string _newCode = string.Empty;

    partial void OnSelectedProductChanged(ProductDto? value)
    {
        if (value is null) return;
        Code = value.Code;
        Name = value.Name;
        Unit = value.Unit;
        Size = value.Size ?? string.Empty;
        Category = value.Category ?? string.Empty;
        NewCode = value.Code;
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        try
        {
            var items = await _productService.SearchAsync(Keyword);
            Products.Clear();
            foreach (var item in items)
                Products.Add(item);

            Message = $"Đã tải {Products.Count} sản phẩm.";
        }
        catch (Exception ex)
        {
            Message = ex.Message;
        }
    }

    [RelayCommand]
    private async Task AddAsync()
    {
        try
        {
            var created = await _productService.CreateAsync(new CreateProductRequest
            {
                Code = Code,
                Name = Name,
                Unit = Unit,
                Size = string.IsNullOrWhiteSpace(Size) ? null : Size,
                Category = string.IsNullOrWhiteSpace(Category) ? null : Category
            });

            Products.Add(created);
            SelectedProduct = created;
            Message = "Đã thêm sản phẩm.";
        }
        catch (Exception ex)
        {
            Message = ex.Message;
        }
    }

    [RelayCommand]
    private async Task UpdateAsync()
    {
        if (SelectedProduct is null)
        {
            Message = "Vui lòng chọn sản phẩm cần sửa.";
            return;
        }

        try
        {
            await _productService.UpdateAsync(new UpdateProductRequest
            {
                Id = SelectedProduct.Id,
                Name = Name,
                Unit = Unit,
                Size = string.IsNullOrWhiteSpace(Size) ? null : Size,
                Category = string.IsNullOrWhiteSpace(Category) ? null : Category
            });

            await LoadAsync();
            Message = "Đã cập nhật sản phẩm.";
        }
        catch (Exception ex)
        {
            Message = ex.Message;
        }
    }

    [RelayCommand]
    private async Task DeleteAsync()
    {
        if (SelectedProduct is null)
        {
            Message = "Vui lòng chọn sản phẩm cần xóa.";
            return;
        }

        try
        {
            await _productService.DeleteAsync(SelectedProduct.Id);
            await LoadAsync();
            Message = "Đã xóa sản phẩm.";
        }
        catch (Exception ex)
        {
            Message = ex.Message;
        }
    }

    [RelayCommand]
    private async Task ChangeCodeAsync()
    {
        if (SelectedProduct is null)
        {
            Message = "Vui lòng chọn sản phẩm cần đổi mã.";
            return;
        }

        try
        {
            await _productService.ChangeCodeAsync(SelectedProduct.Id, NewCode);
            await LoadAsync();
            Message = "Đổi mã thành công.";
        }
        catch (Exception ex)
        {
            Message = ex.Message;
        }
    }

    [RelayCommand]
    private void Back()
    {
        _goBack();
    }
}
