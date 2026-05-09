using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InventoryApp.Core.DTOs;
using InventoryApp.Core.Interfaces;

namespace InventoryApp.UI.ViewModels;

public partial class ReportViewModel : ViewModelBase
{
    private readonly IReportService _reportService;
    private readonly IExportService _exportService;
    private readonly Action _goBack;

    public ReportViewModel(IReportService reportService, IExportService exportService, Action goBack)
    {
        _reportService = reportService;
        _exportService = exportService;
        _goBack = goBack;
        ApplyPreset("THANG_NAY");
        ExportDirectory = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "export"));
    }

    public ObservableCollection<InventoryReportRowDto> Rows { get; } = new();
    public ObservableCollection<HoldReportRowDto> HoldRows { get; } = new();
    public ObservableCollection<PersonSummaryRowDto> SaleRows { get; } = new();
    public ObservableCollection<PersonSummaryRowDto> RequesterRows { get; } = new();
    public IReadOnlyList<string> Presets { get; } = new[] { "TUY_CHON", "THANG_NAY", "QUY_NAY", "NAM_NAY", "30_NGAY_GAN_NHAT" };

    [ObservableProperty] private DateTime _fromDate;
    [ObservableProperty] private DateTime _toDate;
    [ObservableProperty] private string _exportDirectory = string.Empty;
    [ObservableProperty] private string _message = "";
    [ObservableProperty] private string _selectedPreset = "THANG_NAY";

    partial void OnSelectedPresetChanged(string value)
    {
        ApplyPreset(value);
    }

    [RelayCommand]
    private async Task RunReportAsync()
    {
        if (FromDate.Date > ToDate.Date)
        {
            Message = "Khoảng ngày không hợp lệ.";
            return;
        }

        var rows = await _reportService.GetInventoryReportAsync(FromDate, ToDate);
        Rows.Clear();
        foreach (var row in rows) Rows.Add(row);

        var hold = await _reportService.GetHoldReportAsync(FromDate, ToDate);
        HoldRows.Clear();
        foreach (var row in hold) HoldRows.Add(row);

        var sale = await _reportService.GetSaleSummaryReportAsync(FromDate, ToDate);
        SaleRows.Clear();
        foreach (var row in sale) SaleRows.Add(row);

        var requester = await _reportService.GetRequesterSummaryReportAsync(FromDate, ToDate);
        RequesterRows.Clear();
        foreach (var row in requester) RequesterRows.Add(row);

        Message = $"Đã tạo báo cáo {FromDate:dd/MM/yyyy} - {ToDate:dd/MM/yyyy}.";
    }

    [RelayCommand]
    private async Task ExportExcelAsync()
    {
        try
        {
            var f1 = await _exportService.ExportInventoryReportAsync(FromDate, ToDate, ExportDirectory);
            var f2 = await _exportService.ExportHoldReportAsync(FromDate, ToDate, ExportDirectory);
            var f3 = await _exportService.ExportSaleSummaryReportAsync(FromDate, ToDate, ExportDirectory);
            var f4 = await _exportService.ExportRequesterSummaryReportAsync(FromDate, ToDate, ExportDirectory);
            Message = $"Đã xuất:\n- {f1}\n- {f2}\n- {f3}\n- {f4}";
        }
        catch (Exception ex)
        {
            Message = ex.Message;
        }
    }

    [RelayCommand]
    private void Back() => _goBack();

    private void ApplyPreset(string preset)
    {
        var today = DateTime.Today;
        switch (preset)
        {
            case "THANG_NAY":
                FromDate = new DateTime(today.Year, today.Month, 1);
                ToDate = today;
                break;
            case "QUY_NAY":
                var quarter = (today.Month - 1) / 3;
                var startMonth = quarter * 3 + 1;
                FromDate = new DateTime(today.Year, startMonth, 1);
                ToDate = today;
                break;
            case "NAM_NAY":
                FromDate = new DateTime(today.Year, 1, 1);
                ToDate = today;
                break;
            case "30_NGAY_GAN_NHAT":
                FromDate = today.AddDays(-29);
                ToDate = today;
                break;
            default:
                break;
        }
    }
}
