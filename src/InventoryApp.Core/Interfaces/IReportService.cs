using InventoryApp.Core.DTOs;

namespace InventoryApp.Core.Interfaces;

public interface IReportService
{
    Task<IReadOnlyList<InventoryReportRowDto>> GetInventoryReportAsync(DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<HoldReportRowDto>> GetHoldReportAsync(DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PersonSummaryRowDto>> GetSaleSummaryReportAsync(DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PersonSummaryRowDto>> GetRequesterSummaryReportAsync(DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default);
}
