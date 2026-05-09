namespace InventoryApp.Core.Interfaces;

public interface IExportService
{
    Task<string> ExportInventoryReportAsync(DateTime fromDate, DateTime toDate, string outputDirectory, CancellationToken cancellationToken = default);
    Task<string> ExportHoldReportAsync(DateTime fromDate, DateTime toDate, string outputDirectory, CancellationToken cancellationToken = default);
    Task<string> ExportSaleSummaryReportAsync(DateTime fromDate, DateTime toDate, string outputDirectory, CancellationToken cancellationToken = default);
    Task<string> ExportRequesterSummaryReportAsync(DateTime fromDate, DateTime toDate, string outputDirectory, CancellationToken cancellationToken = default);
}
