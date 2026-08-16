using SPIP.Application.DTOs.ReconciliationReport;
using SPIP.Domain.Entities;

namespace SPIP.Application.Interfaces.Repositories;

public interface IReconciliationReportRepository
{
    Task<(IReadOnlyList<ReconciliationReportSummaryDto> Items, int TotalCount)> GetPageAsync(
        ReconciliationReportParameters parameters,
        CancellationToken cancellationToken = default);

    Task<Invoice?> GetDetailAsync(int invoiceId, CancellationToken cancellationToken = default);
}
