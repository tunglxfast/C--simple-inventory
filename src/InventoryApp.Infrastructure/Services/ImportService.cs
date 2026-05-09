using ClosedXML.Excel;
using InventoryApp.Core.DTOs;
using InventoryApp.Core.Entities;
using InventoryApp.Core.Enums;
using InventoryApp.Core.Interfaces;

namespace InventoryApp.Infrastructure.Services;

public sealed class ImportService : IImportService
{
    private readonly IProductService _productService;
    private readonly IOpeningStockService _openingStockService;
    private readonly IStockDocumentService _stockDocumentService;

    public ImportService(
        IProductService productService,
        IOpeningStockService openingStockService,
        IStockDocumentService stockDocumentService)
    {
        _productService = productService;
        _openingStockService = openingStockService;
        _stockDocumentService = stockDocumentService;
    }

    public async Task<ImportResult> ImportProductsAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var result = new ImportResult();
        using var wb = new XLWorkbook(filePath);
        var ws = wb.Worksheets.First();
        var map = BuildHeaderMap(ws.Row(1));
        var lastRow = ws.LastRowUsed()?.RowNumber() ?? 1;

        for (var r = 2; r <= lastRow; r++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            result.TotalRows++;

            try
            {
                var code = GetValue(ws, r, map, "ma_hang", "product_code");
                var name = GetValue(ws, r, map, "ten_hang", "name");
                var unit = GetValue(ws, r, map, "dvt", "unit");
                var size = GetValue(ws, r, map, "size");
                var category = GetValue(ws, r, map, "nhom_hang", "category");
                var barcode = GetValue(ws, r, map, "ma_vach", "barcode");
                var color = GetValue(ws, r, map, "mau_sac", "color");
                var note = GetValue(ws, r, map, "ghi_chu", "note");

                await _productService.CreateAsync(new CreateProductRequest
                {
                    Code = code,
                    Name = name,
                    Unit = unit,
                    Size = NullIfWhite(size),
                    Category = NullIfWhite(category),
                    Barcode = NullIfWhite(barcode),
                    Color = NullIfWhite(color),
                    Note = NullIfWhite(note)
                }, cancellationToken);

                result.SuccessRows++;
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Row {r}: {ex.Message}");
            }
        }

        return result;
    }

    public async Task<ImportResult> ImportOpeningStockAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var result = new ImportResult();
        using var wb = new XLWorkbook(filePath);
        var ws = wb.Worksheets.First();
        var map = BuildHeaderMap(ws.Row(1));
        var lastRow = ws.LastRowUsed()?.RowNumber() ?? 1;

        var products = await _productService.SearchAsync(null, cancellationToken);
        var byCode = products.ToDictionary(x => x.Code.Trim().ToLowerInvariant(), x => x.Id);

        for (var r = 2; r <= lastRow; r++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            result.TotalRows++;

            try
            {
                var code = GetValue(ws, r, map, "ma_hang", "product_code").Trim().ToLowerInvariant();
                var qtyRaw = GetValue(ws, r, map, "so_luong_dau_ky", "qty");
                var note = GetValue(ws, r, map, "ghi_chu", "note");

                if (!byCode.TryGetValue(code, out var productId))
                    throw new InvalidOperationException("Không tìm thấy mã sản phẩm.");

                if (!decimal.TryParse(qtyRaw, out var qty) || qty <= 0)
                    throw new InvalidOperationException("Số lượng đầu kỳ không hợp lệ.");

                await _openingStockService.AddAsync(new CreateOpeningStockRequest
                {
                    ProductId = productId,
                    Qty = qty,
                    Note = NullIfWhite(note)
                }, cancellationToken);

                result.SuccessRows++;
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Row {r}: {ex.Message}");
            }
        }

        return result;
    }

    public async Task<ImportResult> ImportTransactionsAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var result = new ImportResult();
        using var wb = new XLWorkbook(filePath);
        var ws = wb.Worksheets.First();
        var map = BuildHeaderMap(ws.Row(1));
        var lastRow = ws.LastRowUsed()?.RowNumber() ?? 1;

        var products = await _productService.SearchAsync(null, cancellationToken);
        var byCode = products.ToDictionary(x => x.Code.Trim().ToLowerInvariant(), x => x.Id);

        var groups = new Dictionary<string, List<TransactionRow>>(StringComparer.OrdinalIgnoreCase);

        for (var r = 2; r <= lastRow; r++)
        {
            result.TotalRows++;
            try
            {
                var row = ParseTransactionRow(ws, r, map);
                if (!groups.ContainsKey(row.DocNo)) groups[row.DocNo] = new List<TransactionRow>();
                groups[row.DocNo].Add(row);
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Row {r}: {ex.Message}");
            }
        }

        foreach (var pair in groups)
        {
            var rows = pair.Value;
            try
            {
                var first = rows[0];
                var doc = await _stockDocumentService.CreateDraftAsync(new StockDocument
                {
                    DocType = ParseDocType(first.DocType),
                    DocDate = ParseDocDate(first.DocDate),
                    CustomerName = NullIfWhite(first.CustomerName),
                    SaleEmployeeName = NullIfWhite(first.SaleEmployeeName),
                    RequestEmployeeName = NullIfWhite(first.RequestEmployeeName),
                    Area = NullIfWhite(first.Area),
                    Address = NullIfWhite(first.Address),
                    Phone = NullIfWhite(first.Phone),
                    PaymentMethod = ParsePaymentMethod(first.PaymentMethod),
                    ReferenceDocNo = NullIfWhite(first.ReferenceDocNo),
                    Note = NullIfWhite(first.Note)
                }, cancellationToken);

                foreach (var line in rows)
                {
                    var key = line.ProductCode.Trim().ToLowerInvariant();
                    if (!byCode.TryGetValue(key, out var productId))
                        throw new InvalidOperationException($"Không tìm thấy sản phẩm {line.ProductCode}.");

                    if (!decimal.TryParse(line.Qty, out var qty) || qty <= 0)
                        throw new InvalidOperationException("Số lượng không hợp lệ.");

                    var effect = ParseStockEffectType(line.StockEffectType);
                    await _stockDocumentService.AddItemAsync(doc.Id, new StockDocumentItem
                    {
                        DocumentId = doc.Id,
                        ProductId = productId,
                        Qty = qty,
                        StockEffectType = effect,
                        ItemStatus = effect == StockEffectType.Hold ? ItemStatus.Hold : null,
                        Note = NullIfWhite(line.Note)
                    }, cancellationToken);
                }

                if (string.Equals(first.DocumentStatus, "CONFIRMED", StringComparison.OrdinalIgnoreCase))
                {
                    await _stockDocumentService.ConfirmAsync(doc.Id, cancellationToken);
                }

                result.SuccessRows += rows.Count;
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Doc {pair.Key}: {ex.Message}");
            }
        }

        return result;
    }

    private static Dictionary<string, int> BuildHeaderMap(IXLRow headerRow)
    {
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var cell in headerRow.CellsUsed())
        {
            var key = NormalizeHeader(cell.GetString());
            if (!string.IsNullOrWhiteSpace(key) && !map.ContainsKey(key))
                map[key] = cell.Address.ColumnNumber;
        }

        return map;
    }

    private static string GetValue(IXLWorksheet ws, int row, Dictionary<string, int> map, params string[] keys)
    {
        foreach (var key in keys)
        {
            var normalized = NormalizeHeader(key);
            if (map.TryGetValue(normalized, out var col))
                return ws.Cell(row, col).GetString().Trim();
        }

        throw new InvalidOperationException($"Thiếu cột bắt buộc: {string.Join("/", keys)}");
    }

    private static string NormalizeHeader(string text)
        => text.Trim().ToLowerInvariant();

    private static string? NullIfWhite(string? input)
        => string.IsNullOrWhiteSpace(input) ? null : input.Trim();

    private static DateTime ParseDocDate(string value)
    {
        if (DateTime.TryParseExact(value.Trim(), "dd/MM/yyyy", null, System.Globalization.DateTimeStyles.None, out var full))
            return full;

        if (DateTime.TryParseExact(value.Trim(), "dd/MM", null, System.Globalization.DateTimeStyles.None, out var shortDate))
            return new DateTime(DateTime.Today.Year, shortDate.Month, shortDate.Day);

        throw new InvalidOperationException("Ngày chứng từ sai format (dd/MM/yyyy hoặc dd/MM).");
    }

    private static DocumentType ParseDocType(string value)
    {
        return value.Trim().ToUpperInvariant() switch
        {
            "IMPORT" => DocumentType.Import,
            "EXPORT" => DocumentType.Export,
            "RETURN" => DocumentType.Return,
            "ADJUSTMENT" => DocumentType.Adjustment,
            _ => throw new InvalidOperationException("doc_type không hợp lệ.")
        };
    }

    private static StockEffectType ParseStockEffectType(string value)
    {
        return value.Trim().ToUpperInvariant() switch
        {
            "IMPORT" => StockEffectType.Import,
            "EXPORT" => StockEffectType.Export,
            "HOLD" => StockEffectType.Hold,
            "RETURN" => StockEffectType.Return,
            "DAMAGE" => StockEffectType.Damage,
            _ => throw new InvalidOperationException("stock_effect_type không hợp lệ.")
        };
    }

    private static PaymentMethod? ParsePaymentMethod(string value)
    {
        var v = value.Trim().ToUpperInvariant();
        return v switch
        {
            "" => null,
            "CASH" => PaymentMethod.Cash,
            "BANK_TRANSFER" => PaymentMethod.BankTransfer,
            _ => null
        };
    }

    private static TransactionRow ParseTransactionRow(IXLWorksheet ws, int row, Dictionary<string, int> map)
    {
        return new TransactionRow
        {
            DocNo = GetValue(ws, row, map, "so_phieu", "doc_no"),
            DocDate = GetValue(ws, row, map, "ngay_lap_phieu", "doc_date"),
            DocType = GetValue(ws, row, map, "loai_phieu", "doc_type"),
            DocumentStatus = GetValue(ws, row, map, "trang_thai_phieu", "document_status"),
            StockEffectType = GetValue(ws, row, map, "nghiep_vu", "stock_effect_type"),
            ProductCode = GetValue(ws, row, map, "ma_hang", "product_code"),
            Qty = GetValue(ws, row, map, "so_luong", "qty"),
            SaleEmployeeName = GetValue(ws, row, map, "nv_ban_hang", "sale_employee_name"),
            RequestEmployeeName = GetValue(ws, row, map, "nv_de_xuat", "request_employee_name"),
            CustomerName = GetValue(ws, row, map, "khach_hang", "customer_name"),
            Area = GetValue(ws, row, map, "khu_vuc", "area"),
            Address = GetValue(ws, row, map, "dia_chi", "address"),
            Phone = GetValue(ws, row, map, "dien_thoai", "phone"),
            PaymentMethod = GetValue(ws, row, map, "hinh_thuc_tt", "payment_method"),
            ReferenceDocNo = GetValue(ws, row, map, "so_tham_chieu", "reference_doc_no"),
            Note = GetValue(ws, row, map, "ghi_chu", "note")
        };
    }

    private sealed class TransactionRow
    {
        public string DocNo { get; set; } = string.Empty;
        public string DocDate { get; set; } = string.Empty;
        public string DocType { get; set; } = string.Empty;
        public string DocumentStatus { get; set; } = string.Empty;
        public string StockEffectType { get; set; } = string.Empty;
        public string ProductCode { get; set; } = string.Empty;
        public string Qty { get; set; } = string.Empty;
        public string SaleEmployeeName { get; set; } = string.Empty;
        public string RequestEmployeeName { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string Area { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = string.Empty;
        public string ReferenceDocNo { get; set; } = string.Empty;
        public string Note { get; set; } = string.Empty;
    }
}
