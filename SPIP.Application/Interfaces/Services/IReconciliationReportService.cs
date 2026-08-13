using SPIP.Application.DTOs.ReconciliationReport;
using SPIP.Shared.Pagination;
using SPIP.Shared.Result;

namespace SPIP.Application.Interfaces.Services;

public interface IReconciliationReportService
{
    Task<Result<PagedResult<ReconciliationReportSummaryDto>>> GetPageAsync(
        ReconciliationReportParameters parameters,
        CancellationToken cancellationToken = default);

    Task<Result<ManagerReconciliationReportDto>> GetDetailAsync(
        int invoiceId,
        CancellationToken cancellationToken = default);
}
