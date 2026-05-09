using ClosedXML.Excel;
using InventoryApp.Core.Interfaces;

namespace InventoryApp.Infrastructure.Services;

public sealed class ExportService : IExportService
{
    private readonly IReportService _reportService;

    public ExportService(IReportService reportService)
    {
        _reportService = reportService;
    }

    public async Task<string> ExportInventoryReportAsync(DateTime fromDate, DateTime toDate, string outputDirectory, CancellationToken cancellationToken = default)
    {
        var rows = await _reportService.GetInventoryReportAsync(fromDate, toDate, cancellationToken);
        return Export(outputDirectory, $"bao-cao-xuat-nhap-ton-{fromDate:yyyyMMdd}-{toDate:yyyyMMdd}.xlsx", ws =>
        {
            var headers = new[] { "Mã hàng", "Tên hàng", "Đầu kỳ", "Nhập", "Xuất", "Trả", "Hư hỏng", "Tồn cuối" };
            WriteHeader(ws, headers);
            var r = 2;
            foreach (var x in rows)
            {
                ws.Cell(r, 1).Value = x.ProductCode;
                ws.Cell(r, 2).Value = x.ProductName;
                ws.Cell(r, 3).Value = x.OpeningQty;
                ws.Cell(r, 4).Value = x.ImportQty;
                ws.Cell(r, 5).Value = x.ExportQty;
                ws.Cell(r, 6).Value = x.ReturnQty;
                ws.Cell(r, 7).Value = x.DamageQty;
                ws.Cell(r, 8).Value = x.ClosingQty;
                r++;
            }
            FinalizeSheet(ws, r - 1, headers.Length);
        });
    }

    public async Task<string> ExportHoldReportAsync(DateTime fromDate, DateTime toDate, string outputDirectory, CancellationToken cancellationToken = default)
    {
        var rows = await _reportService.GetHoldReportAsync(fromDate, toDate, cancellationToken);
        return Export(outputDirectory, $"bao-cao-hold-{fromDate:yyyyMMdd}-{toDate:yyyyMMdd}.xlsx", ws =>
        {
            var headers = new[] { "Số phiếu", "Item ID", "Mã hàng", "Tên hàng", "Số lượng", "Trạng thái", "Ngày" };
            WriteHeader(ws, headers);
            var r = 2;
            foreach (var x in rows)
            {
                ws.Cell(r, 1).Value = x.DocNo;
                ws.Cell(r, 2).Value = x.ItemId;
                ws.Cell(r, 3).Value = x.ProductCode;
                ws.Cell(r, 4).Value = x.ProductName;
                ws.Cell(r, 5).Value = x.Qty;
                ws.Cell(r, 6).Value = x.ItemStatus;
                ws.Cell(r, 7).Value = x.DocDate;
                r++;
            }
            FinalizeSheet(ws, r - 1, headers.Length);
        });
    }

    public async Task<string> ExportSaleSummaryReportAsync(DateTime fromDate, DateTime toDate, string outputDirectory, CancellationToken cancellationToken = default)
    {
        var rows = await _reportService.GetSaleSummaryReportAsync(fromDate, toDate, cancellationToken);
        return ExportPersonSummary(outputDirectory, $"bao-cao-sale-{fromDate:yyyyMMdd}-{toDate:yyyyMMdd}.xlsx", rows);
    }

    public async Task<string> ExportRequesterSummaryReportAsync(DateTime fromDate, DateTime toDate, string outputDirectory, CancellationToken cancellationToken = default)
    {
        var rows = await _reportService.GetRequesterSummaryReportAsync(fromDate, toDate, cancellationToken);
        return ExportPersonSummary(outputDirectory, $"bao-cao-de-xuat-{fromDate:yyyyMMdd}-{toDate:yyyyMMdd}.xlsx", rows);
    }

    private static string ExportPersonSummary(string outputDirectory, string fileName, IReadOnlyList<Core.DTOs.PersonSummaryRowDto> rows)
    {
        return Export(outputDirectory, fileName, ws =>
        {
            var headers = new[] { "Tên", "Số lượng xuất", "Số chứng từ" };
            WriteHeader(ws, headers);
            var r = 2;
            foreach (var x in rows)
            {
                ws.Cell(r, 1).Value = x.PersonName;
                ws.Cell(r, 2).Value = x.ExportQty;
                ws.Cell(r, 3).Value = x.DocumentCount;
                r++;
            }
            FinalizeSheet(ws, r - 1, headers.Length);
        });
    }

    private static string Export(string outputDirectory, string fileName, Action<IXLWorksheet> fill)
    {
        Directory.CreateDirectory(outputDirectory);
        var fullPath = Path.Combine(outputDirectory, fileName);
        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("Report");
        fill(ws);
        wb.SaveAs(fullPath);
        return fullPath;
    }

    private static void WriteHeader(IXLWorksheet ws, IReadOnlyList<string> headers)
    {
        for (var i = 0; i < headers.Count; i++)
        {
            ws.Cell(1, i + 1).Value = headers[i];
            ws.Cell(1, i + 1).Style.Font.Bold = true;
            ws.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#E8F1FF");
        }
    }

    private static void FinalizeSheet(IXLWorksheet ws, int lastRow, int lastCol)
    {
        ws.Columns().AdjustToContents();
        var endRow = Math.Max(1, lastRow);
        ws.Range(1, 1, endRow, lastCol).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        ws.Range(1, 1, endRow, lastCol).Style.Border.InsideBorder = XLBorderStyleValues.Thin;
    }
}
