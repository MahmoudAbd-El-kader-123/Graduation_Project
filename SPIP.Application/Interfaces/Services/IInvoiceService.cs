using SPIP.Application.DTOs.Invoice;
using SPIP.Shared.Pagination;
using SPIP.Shared.Result;

namespace SPIP.Application.Interfaces.Services;

public interface IInvoiceService
{
    Task<Result<InvoiceUploadResultDto>> UploadInvoiceAsync(UploadInvoiceRequest request, CancellationToken cancellationToken = default);
    Task<Result<InvoiceDetailDto>> GetByIdAsync(int invoiceId, CancellationToken cancellationToken = default);
    Task<Result<InvoiceReconciliationDto>> GetReconciliationAsync(int invoiceId, CancellationToken cancellationToken = default);
    Task<Result<(Stream FileStream, string ContentType, string FileName)>> DownloadFileAsync(int invoiceId, CancellationToken cancellationToken = default);
    Task<Result<PagedResult<InvoiceListItemDto>>> GetAllPagedAsync(InvoiceListParameters parameters, CancellationToken cancellationToken = default);
}
