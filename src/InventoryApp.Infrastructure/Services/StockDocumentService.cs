using Dapper;
using InventoryApp.Core.Entities;
using InventoryApp.Core.Enums;
using InventoryApp.Core.Interfaces;
using InventoryApp.Infrastructure.Persistence;

namespace InventoryApp.Infrastructure.Services;

public sealed class StockDocumentService : IStockDocumentService
{
    private readonly SqliteConnectionFactory _connectionFactory;
    private readonly IDocNoGenerator _docNoGenerator;
    private readonly IClock _clock;

    public StockDocumentService(SqliteConnectionFactory connectionFactory, IDocNoGenerator docNoGenerator, IClock clock)
    {
        _connectionFactory = connectionFactory;
        _docNoGenerator = docNoGenerator;
        _clock = clock;
    }

    public async Task<StockDocument> CreateDraftAsync(StockDocument document, CancellationToken cancellationToken = default)
    {
        document.DocNo = await _docNoGenerator.GenerateAsync(document.DocType, document.DocDate, cancellationToken);
        document.DocumentStatus = DocumentStatus.Draft;
        document.CreatedAt = _clock.UtcNow;
        document.UpdatedAt = _clock.UtcNow;

        const string sql = @"
INSERT INTO stock_documents(
    doc_no, doc_type, reference_doc_no, reversed_document_id, customer_name,
    sale_employee_name, request_employee_name, area, address, phone,
    payment_method, document_status, note, reporting_period_id, doc_date,
    created_at, updated_at)
VALUES (
    @DocNo, @DocType, @ReferenceDocNo, @ReversedDocumentId, @CustomerName,
    @SaleEmployeeName, @RequestEmployeeName, @Area, @Address, @Phone,
    @PaymentMethod, @DocumentStatus, @Note, @ReportingPeriodId, @DocDate,
    @CreatedAt, @UpdatedAt
);
SELECT last_insert_rowid();";

        await using var conn = _connectionFactory.CreateConnection();
        await conn.OpenAsync(cancellationToken);
        document.Id = await conn.ExecuteScalarAsync<long>(sql, ToDocumentParams(document));

        return document;
    }

    public async Task AddItemAsync(long documentId, StockDocumentItem item, CancellationToken cancellationToken = default)
    {
        const string checkSql = "SELECT document_status FROM stock_documents WHERE id=@documentId;";
        const string insertSql = @"
INSERT INTO stock_document_items(document_id, product_id, qty, stock_effect_type, item_status, note)
VALUES(@DocumentId, @ProductId, @Qty, @StockEffectType, @ItemStatus, @Note);";

        await using var conn = _connectionFactory.CreateConnection();
        await conn.OpenAsync(cancellationToken);

        var status = await conn.ExecuteScalarAsync<string?>(checkSql, new { documentId });
        if (!string.Equals(status, "DRAFT", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Chỉ thêm dòng khi phiếu đang ở trạng thái DRAFT.");

        await conn.ExecuteAsync(insertSql, new
        {
            DocumentId = documentId,
            item.ProductId,
            Qty = item.Qty,
            StockEffectType = item.StockEffectType.ToString().ToUpperInvariant(),
            ItemStatus = item.ItemStatus?.ToString().ToUpperInvariant(),
            item.Note
        });
    }

    public async Task ConfirmAsync(long documentId, CancellationToken cancellationToken = default)
    {
        await using var conn = _connectionFactory.CreateConnection();
        await conn.OpenAsync(cancellationToken);

        var current = await conn.ExecuteScalarAsync<string?>(
            "SELECT document_status FROM stock_documents WHERE id = @documentId;",
            new { documentId });

        if (current is null) throw new InvalidOperationException("Không tìm thấy phiếu.");
        if (!string.Equals(current, "DRAFT", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Chỉ được xác nhận phiếu ở trạng thái DRAFT.");

        var rows = await LoadDocItemsAsync(conn, documentId);
        await ValidateNoNegativeStockAsync(conn, rows);

        await conn.ExecuteAsync(
            "UPDATE stock_documents SET document_status = @status, updated_at = @updatedAt WHERE id = @documentId;",
            new
            {
                documentId,
                status = DocumentStatus.Confirmed.ToString().ToUpperInvariant(),
                updatedAt = _clock.UtcNow.ToString("O")
            });
    }

    public async Task<IReadOnlyList<StockDocumentItem>> GetHoldingItemsAsync(CancellationToken cancellationToken = default)
    {
        const string sql = @"
SELECT sdi.id AS Id, sdi.document_id AS DocumentId, sdi.product_id AS ProductId,
       sdi.qty AS Qty, sdi.stock_effect_type AS StockEffectType,
       sdi.item_status AS ItemStatus, sdi.note AS Note
FROM stock_document_items sdi
JOIN stock_documents sd ON sd.id = sdi.document_id
WHERE sd.document_status = 'CONFIRMED'
  AND sdi.stock_effect_type = 'HOLD'
  AND sdi.item_status = 'HOLD'
ORDER BY sdi.id DESC;";

        await using var conn = _connectionFactory.CreateConnection();
        await conn.OpenAsync(cancellationToken);
        var rows = await conn.QueryAsync<DocItemRow>(sql);
        return rows.Select(MapItem).ToList();
    }

    public Task MarkHoldItemAsSoldAsync(long itemId, CancellationToken cancellationToken = default)
        => UpdateHoldItemStatusAsync(itemId, "SOLD", cancellationToken);

    public Task MarkHoldItemAsReturnedAsync(long itemId, CancellationToken cancellationToken = default)
        => UpdateHoldItemStatusAsync(itemId, "RETURNED", cancellationToken);

    public async Task<IReadOnlyList<StockDocument>> GetRecentConfirmedDocumentsAsync(CancellationToken cancellationToken = default)
    {
        const string sql = @"
SELECT id AS Id, doc_no AS DocNo, doc_type AS DocType,
       reference_doc_no AS ReferenceDocNo, reversed_document_id AS ReversedDocumentId,
       customer_name AS CustomerName, sale_employee_name AS SaleEmployeeName,
       request_employee_name AS RequestEmployeeName, area AS Area, address AS Address,
       phone AS Phone, payment_method AS PaymentMethod, document_status AS DocumentStatus,
       note AS Note, reporting_period_id AS ReportingPeriodId,
       doc_date AS DocDate, created_at AS CreatedAt, updated_at AS UpdatedAt
FROM stock_documents
WHERE document_status = 'CONFIRMED'
ORDER BY id DESC
LIMIT 100;";

        await using var conn = _connectionFactory.CreateConnection();
        await conn.OpenAsync(cancellationToken);
        var rows = await conn.QueryAsync<StockDocumentRow>(sql);
        return rows.Select(MapDocument).ToList();
    }

    public async Task<long> CancelWithReversalAsync(long documentId, string? reason, CancellationToken cancellationToken = default)
    {
        await using var conn = _connectionFactory.CreateConnection();
        await conn.OpenAsync(cancellationToken);
        await using var tx = await conn.BeginTransactionAsync(cancellationToken);

        var original = await conn.QueryFirstOrDefaultAsync<StockDocumentRow>(@"
SELECT id AS Id, doc_no AS DocNo, doc_type AS DocType,
       reference_doc_no AS ReferenceDocNo, reversed_document_id AS ReversedDocumentId,
       customer_name AS CustomerName, sale_employee_name AS SaleEmployeeName,
       request_employee_name AS RequestEmployeeName, area AS Area, address AS Address,
       phone AS Phone, payment_method AS PaymentMethod, document_status AS DocumentStatus,
       note AS Note, reporting_period_id AS ReportingPeriodId,
       doc_date AS DocDate, created_at AS CreatedAt, updated_at AS UpdatedAt
FROM stock_documents WHERE id = @documentId LIMIT 1;", new { documentId }, tx);

        if (original is null) throw new InvalidOperationException("Không tìm thấy phiếu cần hủy.");
        if (!string.Equals(original.DocumentStatus, "CONFIRMED", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Chỉ được hủy phiếu ở trạng thái CONFIRMED.");

        var existsReversal = await conn.ExecuteScalarAsync<long>(
            "SELECT COUNT(1) FROM stock_documents WHERE reversed_document_id = @documentId;",
            new { documentId }, tx);
        if (existsReversal > 0) throw new InvalidOperationException("Phiếu này đã có phiếu đảo, không thể hủy lặp.");

        var items = (await conn.QueryAsync<DocItemRow>(@"
SELECT id AS Id, document_id AS DocumentId, product_id AS ProductId,
       qty AS Qty, stock_effect_type AS StockEffectType, item_status AS ItemStatus, note AS Note
FROM stock_document_items
WHERE document_id = @documentId;", new { documentId }, tx)).ToList();

        var reversalDoc = new StockDocument
        {
            DocNo = await _docNoGenerator.GenerateAsync(DocumentType.Adjustment, DateTime.UtcNow, cancellationToken),
            DocType = DocumentType.Adjustment,
            ReferenceDocNo = original.DocNo,
            ReversedDocumentId = original.Id,
            CustomerName = original.CustomerName,
            SaleEmployeeName = original.SaleEmployeeName,
            RequestEmployeeName = original.RequestEmployeeName,
            Area = original.Area,
            Address = original.Address,
            Phone = original.Phone,
            PaymentMethod = ParseEnumNullable<PaymentMethod>(original.PaymentMethod),
            DocumentStatus = DocumentStatus.Confirmed,
            ReportingPeriodId = original.ReportingPeriodId,
            DocDate = DateTime.UtcNow.Date,
            Note = $"REVERSAL of {original.DocNo}. Reason: {reason}".Trim(),
            CreatedAt = _clock.UtcNow,
            UpdatedAt = _clock.UtcNow
        };

        var reversalId = await conn.ExecuteScalarAsync<long>(@"
INSERT INTO stock_documents(
    doc_no, doc_type, reference_doc_no, reversed_document_id, customer_name,
    sale_employee_name, request_employee_name, area, address, phone,
    payment_method, document_status, note, reporting_period_id, doc_date,
    created_at, updated_at)
VALUES (
    @DocNo, @DocType, @ReferenceDocNo, @ReversedDocumentId, @CustomerName,
    @SaleEmployeeName, @RequestEmployeeName, @Area, @Address, @Phone,
    @PaymentMethod, @DocumentStatus, @Note, @ReportingPeriodId, @DocDate,
    @CreatedAt, @UpdatedAt
);
SELECT last_insert_rowid();", ToDocumentParams(reversalDoc), tx);

        foreach (var item in items)
        {
            var effective = item.ToEffectiveDelta();
            var reversalDelta = -effective;
            if (reversalDelta == 0) continue;

            var effectType = reversalDelta > 0 ? "IMPORT" : "EXPORT";
            var qty = Math.Abs(reversalDelta);

            await conn.ExecuteAsync(@"
INSERT INTO stock_document_items(document_id, product_id, qty, stock_effect_type, item_status, note)
VALUES(@DocumentId, @ProductId, @Qty, @StockEffectType, NULL, @Note);", new
            {
                DocumentId = reversalId,
                ProductId = item.ProductId,
                Qty = qty,
                StockEffectType = effectType,
                Note = $"REVERSAL item from {original.DocNo}"
            }, tx);
        }

        await conn.ExecuteAsync(@"
UPDATE stock_documents
SET document_status = 'CANCELLED',
    note = CASE WHEN note IS NULL OR note = '' THEN @cancelNote ELSE note || ' | ' || @cancelNote END,
    updated_at = @updatedAt
WHERE id = @documentId;", new
        {
            documentId,
            cancelNote = $"CANCELLED with reversal {reversalDoc.DocNo}. Reason: {reason}",
            updatedAt = _clock.UtcNow.ToString("O")
        }, tx);

        await tx.CommitAsync(cancellationToken);
        return reversalId;
    }

    private async Task UpdateHoldItemStatusAsync(long itemId, string newStatus, CancellationToken cancellationToken)
    {
        const string sql = @"
UPDATE stock_document_items
SET item_status = @newStatus
WHERE id = @itemId
  AND stock_effect_type = 'HOLD'
  AND item_status = 'HOLD';";

        await using var conn = _connectionFactory.CreateConnection();
        await conn.OpenAsync(cancellationToken);
        var affected = await conn.ExecuteAsync(sql, new { itemId, newStatus });
        if (affected == 0)
            throw new InvalidOperationException("Không tìm thấy dòng HOLD hợp lệ để cập nhật trạng thái.");
    }

    private async Task<List<DocItemRow>> LoadDocItemsAsync(Microsoft.Data.Sqlite.SqliteConnection conn, long documentId)
    {
        var rows = await conn.QueryAsync<DocItemRow>(@"
SELECT id AS Id, document_id AS DocumentId, product_id AS ProductId,
       qty AS Qty, stock_effect_type AS StockEffectType, item_status AS ItemStatus, note AS Note
FROM stock_document_items
WHERE document_id = @documentId;", new { documentId });
        return rows.ToList();
    }

    private async Task ValidateNoNegativeStockAsync(Microsoft.Data.Sqlite.SqliteConnection conn, List<DocItemRow> rows)
    {
        var grouped = rows.GroupBy(x => x.ProductId).ToList();
        foreach (var group in grouped)
        {
            var currentStock = await GetCurrentStockAsync(conn, group.Key);
            var delta = group.Sum(x => x.ToDeltaOnConfirm());
            var projected = currentStock + delta;
            if (projected < 0)
                throw new InvalidOperationException($"Không thể xác nhận phiếu vì sản phẩm ID={group.Key} sẽ âm tồn ({projected}).");
        }
    }

    private async Task<decimal> GetCurrentStockAsync(Microsoft.Data.Sqlite.SqliteConnection conn, long productId)
    {
        const string stockSql = @"
SELECT
    COALESCE((SELECT SUM(os.qty) FROM opening_stock os WHERE os.product_id = @productId), 0)
    + COALESCE((
        SELECT SUM(
            CASE
                WHEN sdi.stock_effect_type = 'IMPORT' THEN sdi.qty
                WHEN sdi.stock_effect_type = 'RETURN' AND COALESCE(sdi.item_status, '') <> 'SOLD' THEN sdi.qty
                WHEN sdi.stock_effect_type = 'EXPORT' THEN -sdi.qty
                WHEN sdi.stock_effect_type = 'HOLD' AND COALESCE(sdi.item_status, '') <> 'RETURNED' THEN -sdi.qty
                WHEN sdi.stock_effect_type = 'DAMAGE' THEN -sdi.qty
                ELSE 0
            END
        )
        FROM stock_document_items sdi
        JOIN stock_documents sd ON sd.id = sdi.document_id
        WHERE sdi.product_id = @productId
          AND sd.document_status = 'CONFIRMED'
    ), 0) AS CurrentStock;";

        return await conn.ExecuteScalarAsync<decimal>(stockSql, new { productId });
    }

    private static object ToDocumentParams(StockDocument document) => new
    {
        document.DocNo,
        DocType = document.DocType.ToString().ToUpperInvariant(),
        document.ReferenceDocNo,
        document.ReversedDocumentId,
        document.CustomerName,
        document.SaleEmployeeName,
        document.RequestEmployeeName,
        document.Area,
        document.Address,
        document.Phone,
        PaymentMethod = document.PaymentMethod?.ToString().ToUpperInvariant(),
        DocumentStatus = document.DocumentStatus.ToString().ToUpperInvariant(),
        document.Note,
        document.ReportingPeriodId,
        DocDate = document.DocDate.ToString("yyyy-MM-dd"),
        CreatedAt = document.CreatedAt.ToString("O"),
        UpdatedAt = document.UpdatedAt.ToString("O")
    };

    private static StockDocumentItem MapItem(DocItemRow r) => new()
    {
        Id = r.Id,
        DocumentId = r.DocumentId,
        ProductId = r.ProductId,
        Qty = r.Qty,
        StockEffectType = Enum.TryParse<StockEffectType>(r.StockEffectType, true, out var st) ? st : StockEffectType.Hold,
        ItemStatus = Enum.TryParse<ItemStatus>(r.ItemStatus ?? "HOLD", true, out var it) ? it : InventoryApp.Core.Enums.ItemStatus.Hold,
        Note = r.Note
    };

    private static StockDocument MapDocument(StockDocumentRow row)
    {
        return new StockDocument
        {
            Id = row.Id,
            DocNo = row.DocNo,
            DocType = ParseEnum<DocumentType>(row.DocType),
            ReferenceDocNo = row.ReferenceDocNo,
            ReversedDocumentId = row.ReversedDocumentId,
            CustomerName = row.CustomerName,
            SaleEmployeeName = row.SaleEmployeeName,
            RequestEmployeeName = row.RequestEmployeeName,
            Area = row.Area,
            Address = row.Address,
            Phone = row.Phone,
            PaymentMethod = ParseEnumNullable<PaymentMethod>(row.PaymentMethod),
            DocumentStatus = ParseEnum<DocumentStatus>(row.DocumentStatus),
            Note = row.Note,
            ReportingPeriodId = row.ReportingPeriodId,
            DocDate = DateTime.TryParse(row.DocDate, out var docDate) ? docDate : DateTime.Today,
            CreatedAt = DateTime.TryParse(row.CreatedAt, out var created) ? created : DateTime.UtcNow,
            UpdatedAt = DateTime.TryParse(row.UpdatedAt, out var updated) ? updated : DateTime.UtcNow
        };
    }

    private static TEnum ParseEnum<TEnum>(string value) where TEnum : struct
        => Enum.TryParse<TEnum>(value, true, out var parsed) ? parsed : default;

    private static TEnum? ParseEnumNullable<TEnum>(string? value) where TEnum : struct
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        return Enum.TryParse<TEnum>(value, true, out var parsed) ? parsed : null;
    }

    private sealed class DocItemRow
    {
        public long Id { get; set; }
        public long DocumentId { get; set; }
        public long ProductId { get; set; }
        public decimal Qty { get; set; }
        public string StockEffectType { get; set; } = string.Empty;
        public string? ItemStatus { get; set; }
        public string? Note { get; set; }

        public decimal ToDeltaOnConfirm()
        {
            return StockEffectType.ToUpperInvariant() switch
            {
                "IMPORT" => Qty,
                "RETURN" => Qty,
                "EXPORT" => -Qty,
                "HOLD" => -Qty,
                "DAMAGE" => -Qty,
                _ => 0
            };
        }

        public decimal ToEffectiveDelta()
        {
            var type = StockEffectType.ToUpperInvariant();
            var status = (ItemStatus ?? string.Empty).ToUpperInvariant();
            return type switch
            {
                "IMPORT" => Qty,
                "RETURN" when status != "SOLD" => Qty,
                "RETURN" => 0,
                "EXPORT" => -Qty,
                "HOLD" when status != "RETURNED" => -Qty,
                "HOLD" => 0,
                "DAMAGE" => -Qty,
                _ => 0
            };
        }
    }

    private sealed class StockDocumentRow
    {
        public long Id { get; set; }
        public string DocNo { get; set; } = string.Empty;
        public string DocType { get; set; } = string.Empty;
        public string? ReferenceDocNo { get; set; }
        public long? ReversedDocumentId { get; set; }
        public string? CustomerName { get; set; }
        public string? SaleEmployeeName { get; set; }
        public string? RequestEmployeeName { get; set; }
        public string? Area { get; set; }
        public string? Address { get; set; }
        public string? Phone { get; set; }
        public string? PaymentMethod { get; set; }
        public string DocumentStatus { get; set; } = string.Empty;
        public string? Note { get; set; }
        public long? ReportingPeriodId { get; set; }
        public string DocDate { get; set; } = string.Empty;
        public string CreatedAt { get; set; } = string.Empty;
        public string UpdatedAt { get; set; } = string.Empty;
    }
}
