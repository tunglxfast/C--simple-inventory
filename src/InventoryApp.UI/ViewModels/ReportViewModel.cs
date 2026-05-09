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

    [ObservableProperty] private DateTimeOffset? _fromDate;
    [ObservableProperty] private DateTimeOffset? _toDate;
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
        if (FromDate is null || ToDate is null || FromDate.Value.Date > ToDate.Value.Date)
        {
            Message = "Khoảng ngày không hợp lệ.";
            return;
        }

        var fromDate = FromDate.Value.DateTime;
        var toDate = ToDate.Value.DateTime;

        var rows = await _reportService.GetInventoryReportAsync(fromDate, toDate);
        Rows.Clear();
        foreach (var row in rows) Rows.Add(row);

        var hold = await _reportService.GetHoldReportAsync(fromDate, toDate);
        HoldRows.Clear();
        foreach (var row in hold) HoldRows.Add(row);

        var sale = await _reportService.GetSaleSummaryReportAsync(fromDate, toDate);
        SaleRows.Clear();
        foreach (var row in sale) SaleRows.Add(row);

        var requester = await _reportService.GetRequesterSummaryReportAsync(fromDate, toDate);
        RequesterRows.Clear();
        foreach (var row in requester) RequesterRows.Add(row);

        Message = $"Đã tạo báo cáo {FromDate:dd/MM/yyyy} - {ToDate:dd/MM/yyyy}.";
    }

    [RelayCommand]
    private async Task ExportExcelAsync()
    {
        try
        {
            if (FromDate is null || ToDate is null)
            {
                Message = "Khoảng ngày không hợp lệ.";
                return;
            }
            var fromDate = FromDate.Value.DateTime;
            var toDate = ToDate.Value.DateTime;
            var f1 = await _exportService.ExportInventoryReportAsync(fromDate, toDate, ExportDirectory);
            var f2 = await _exportService.ExportHoldReportAsync(fromDate, toDate, ExportDirectory);
            var f3 = await _exportService.ExportSaleSummaryReportAsync(fromDate, toDate, ExportDirectory);
            var f4 = await _exportService.ExportRequesterSummaryReportAsync(fromDate, toDate, ExportDirectory);
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
                FromDate = new DateTimeOffset(new DateTime(today.Year, today.Month, 1));
                ToDate = new DateTimeOffset(today);
                break;
            case "QUY_NAY":
                var quarter = (today.Month - 1) / 3;
                var startMonth = quarter * 3 + 1;
                FromDate = new DateTimeOffset(new DateTime(today.Year, startMonth, 1));
                ToDate = new DateTimeOffset(today);
                break;
            case "NAM_NAY":
                FromDate = new DateTimeOffset(new DateTime(today.Year, 1, 1));
                ToDate = new DateTimeOffset(today);
                break;
            case "30_NGAY_GAN_NHAT":
                FromDate = new DateTimeOffset(today.AddDays(-29));
                ToDate = new DateTimeOffset(today);
                break;
            default:
                break;
        }
    }
}
