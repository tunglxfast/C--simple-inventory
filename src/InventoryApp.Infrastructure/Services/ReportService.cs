using Dapper;
using InventoryApp.Core.DTOs;
using InventoryApp.Core.Interfaces;
using InventoryApp.Infrastructure.Persistence;

namespace InventoryApp.Infrastructure.Services;

public sealed class ReportService : IReportService
{
    private readonly SqliteConnectionFactory _connectionFactory;

    public ReportService(SqliteConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<InventoryReportRowDto>> GetInventoryReportAsync(DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default)
    {
        var from = fromDate.Date.ToString("yyyy-MM-dd");
        var to = toDate.Date.ToString("yyyy-MM-dd");

        const string sql = @"
WITH base_products AS (
  SELECT p.id AS ProductId, p.code AS ProductCode, p.name AS ProductName
  FROM products p
),
opening AS (
  SELECT p.id AS ProductId,
         COALESCE((SELECT SUM(os.qty) FROM opening_stock os WHERE os.product_id = p.id), 0)
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
            WHERE sdi.product_id = p.id
              AND sd.document_status = 'CONFIRMED'
              AND sd.doc_date < @from
         ), 0) AS OpeningQty
  FROM products p
),
period_movements AS (
  SELECT sdi.product_id AS ProductId,
         SUM(CASE WHEN sdi.stock_effect_type = 'IMPORT' THEN sdi.qty ELSE 0 END) AS ImportQty,
         SUM(CASE WHEN sdi.stock_effect_type IN ('EXPORT', 'HOLD') THEN sdi.qty ELSE 0 END) AS ExportQty,
         SUM(CASE WHEN sdi.stock_effect_type = 'RETURN' AND COALESCE(sdi.item_status, '') <> 'SOLD' THEN sdi.qty ELSE 0 END) AS ReturnQty,
         SUM(CASE WHEN sdi.stock_effect_type = 'DAMAGE' THEN sdi.qty ELSE 0 END) AS DamageQty
  FROM stock_document_items sdi
  JOIN stock_documents sd ON sd.id = sdi.document_id
  WHERE sd.document_status = 'CONFIRMED'
    AND sd.doc_date >= @from
    AND sd.doc_date <= @to
  GROUP BY sdi.product_id
)
SELECT b.ProductId, b.ProductCode, b.ProductName,
       COALESCE(o.OpeningQty, 0) AS OpeningQty,
       COALESCE(m.ImportQty, 0) AS ImportQty,
       COALESCE(m.ExportQty, 0) AS ExportQty,
       COALESCE(m.ReturnQty, 0) AS ReturnQty,
       COALESCE(m.DamageQty, 0) AS DamageQty,
       COALESCE(o.OpeningQty, 0)
       + COALESCE(m.ImportQty, 0)
       - COALESCE(m.ExportQty, 0)
       + COALESCE(m.ReturnQty, 0)
       - COALESCE(m.DamageQty, 0) AS ClosingQty
FROM base_products b
LEFT JOIN opening o ON o.ProductId = b.ProductId
LEFT JOIN period_movements m ON m.ProductId = b.ProductId
ORDER BY b.ProductCode;";

        await using var conn = _connectionFactory.CreateConnection();
        await conn.OpenAsync(cancellationToken);
        var rows = await conn.QueryAsync<InventoryReportRowDto>(sql, new { from, to });
        return rows.ToList();
    }

    public async Task<IReadOnlyList<HoldReportRowDto>> GetHoldReportAsync(DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default)
    {
        var from = fromDate.Date.ToString("yyyy-MM-dd");
        var to = toDate.Date.ToString("yyyy-MM-dd");
        const string sql = @"
SELECT sd.doc_no AS DocNo,
       sdi.id AS ItemId,
       p.code AS ProductCode,
       p.name AS ProductName,
       sdi.qty AS Qty,
       COALESCE(sdi.item_status, '') AS ItemStatus,
       sd.doc_date AS DocDate
FROM stock_document_items sdi
JOIN stock_documents sd ON sd.id = sdi.document_id
JOIN products p ON p.id = sdi.product_id
WHERE sd.document_status = 'CONFIRMED'
  AND sdi.stock_effect_type = 'HOLD'
  AND sd.doc_date >= @from
  AND sd.doc_date <= @to
ORDER BY sd.doc_date DESC, sdi.id DESC;";

        await using var conn = _connectionFactory.CreateConnection();
        await conn.OpenAsync(cancellationToken);
        var rows = await conn.QueryAsync<HoldReportRowRaw>(sql, new { from, to });
        return rows.Select(x => new HoldReportRowDto
        {
            DocNo = x.DocNo,
            ItemId = x.ItemId,
            ProductCode = x.ProductCode,
            ProductName = x.ProductName,
            Qty = x.Qty,
            ItemStatus = x.ItemStatus,
            DocDate = DateTime.TryParse(x.DocDate, out var d) ? d : DateTime.Today
        }).ToList();
    }

    public Task<IReadOnlyList<PersonSummaryRowDto>> GetSaleSummaryReportAsync(DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default)
        => GetPersonSummaryAsync("sale_employee_name", fromDate, toDate, cancellationToken);

    public Task<IReadOnlyList<PersonSummaryRowDto>> GetRequesterSummaryReportAsync(DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default)
        => GetPersonSummaryAsync("request_employee_name", fromDate, toDate, cancellationToken);

    private async Task<IReadOnlyList<PersonSummaryRowDto>> GetPersonSummaryAsync(string fieldName, DateTime fromDate, DateTime toDate, CancellationToken cancellationToken)
    {
        var from = fromDate.Date.ToString("yyyy-MM-dd");
        var to = toDate.Date.ToString("yyyy-MM-dd");

        var sql = $@"
SELECT COALESCE(NULLIF(sd.{fieldName}, ''), '(Trống)') AS PersonName,
       SUM(CASE WHEN sdi.stock_effect_type IN ('EXPORT', 'HOLD') THEN sdi.qty ELSE 0 END) AS ExportQty,
       COUNT(DISTINCT sd.id) AS DocumentCount
FROM stock_documents sd
JOIN stock_document_items sdi ON sdi.document_id = sd.id
WHERE sd.document_status = 'CONFIRMED'
  AND sd.doc_date >= @from
  AND sd.doc_date <= @to
GROUP BY COALESCE(NULLIF(sd.{fieldName}, ''), '(Trống)')
ORDER BY ExportQty DESC;";

        await using var conn = _connectionFactory.CreateConnection();
        await conn.OpenAsync(cancellationToken);
        var rows = await conn.QueryAsync<PersonSummaryRowDto>(sql, new { from, to });
        return rows.ToList();
    }

    private sealed class HoldReportRowRaw
    {
        public string DocNo { get; set; } = string.Empty;
        public long ItemId { get; set; }
        public string ProductCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public decimal Qty { get; set; }
        public string ItemStatus { get; set; } = string.Empty;
        public string DocDate { get; set; } = string.Empty;
    }
}
