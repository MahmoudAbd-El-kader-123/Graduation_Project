using AutoMapper;
using SPIP.Application.DTOs.Invoice;
using SPIP.Application.DTOs.ReconciliationReport;
using SPIP.Application.Interfaces.Repositories;
using SPIP.Application.Interfaces.Services;
using SPIP.Domain.Entities;
using SPIP.Domain.Enums;
using SPIP.Shared.Pagination;
using SPIP.Shared.Result;

namespace SPIP.Infrastructure.Services;

public sealed class ReconciliationReportService : IReconciliationReportService
{
    private const int MaximumSearchLength = 100;
    private const int MaximumPageNumber = 1_000_000;
    private readonly IReconciliationReportRepository _repository;
    private readonly IMapper _mapper;

    public ReconciliationReportService(IReconciliationReportRepository repository, IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public async Task<Result<PagedResult<ReconciliationReportSummaryDto>>> GetPageAsync(
        ReconciliationReportParameters parameters,
        CancellationToken cancellationToken = default)
    {
        var errors = Validate(parameters);
        if (errors.Count > 0)
            return Result<PagedResult<ReconciliationReportSummaryDto>>.Failure(errors);

        var (items, totalCount) = await _repository.GetPageAsync(parameters, cancellationToken);
        return Result<PagedResult<ReconciliationReportSummaryDto>>.Success(new PagedResult<ReconciliationReportSummaryDto>
        {
            Items = items,
            PageNumber = parameters.PageNumber,
            PageSize = parameters.PageSize,
            TotalCount = totalCount
        });
    }

    public async Task<Result<ManagerReconciliationReportDto>> GetDetailAsync(
        int invoiceId,
        CancellationToken cancellationToken = default)
    {
        var invoice = await _repository.GetDetailAsync(invoiceId, cancellationToken);
        if (invoice is null)
            return Result<ManagerReconciliationReportDto>.Failure("Reconciliation report not found.");

        return Result<ManagerReconciliationReportDto>.Success(new ManagerReconciliationReportDto
        {
            Invoice = MapInvoice(invoice),
            Reconciliation = _mapper.Map<InvoiceReconciliationDto>(invoice),
            StatusMessage = GetSafeStatusMessage(invoice)
        });
    }

    private static List<string> Validate(ReconciliationReportParameters parameters)
    {
        var errors = new List<string>();
        if (parameters.PageNumber is < 1 or > MaximumPageNumber)
            errors.Add($"PageNumber must be between 1 and {MaximumPageNumber}.");
        if (parameters.PageSize is < 1 or > 50)
            errors.Add("PageSize must be between 1 and 50.");
        if (parameters.SearchTerm?.Length > MaximumSearchLength)
            errors.Add($"SearchTerm must not exceed {MaximumSearchLength} characters.");
        if (!string.IsNullOrWhiteSpace(parameters.Status) &&
            !Enum.GetNames<InvoiceStatus>().Contains(parameters.Status, StringComparer.OrdinalIgnoreCase))
            errors.Add("Status is invalid.");
        if (!string.Equals(parameters.SortBy, "uploadedAt", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(parameters.SortBy, "discrepancyCount", StringComparison.OrdinalIgnoreCase))
            errors.Add("SortBy must be uploadedAt or discrepancyCount.");
        if (!string.Equals(parameters.SortDirection, "asc", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(parameters.SortDirection, "desc", StringComparison.OrdinalIgnoreCase))
            errors.Add("SortDirection must be asc or desc.");
        if (parameters.FromDate.HasValue && parameters.ToDate.HasValue && parameters.FromDate > parameters.ToDate)
            errors.Add("FromDate must not be later than ToDate.");
        if (parameters.ToDate?.Date == DateTime.MaxValue.Date)
            errors.Add("ToDate is outside the supported range.");
        return errors;
    }

    private static ReconciliationReportInvoiceDto MapInvoice(Invoice invoice) => new()
    {
        InvoiceId = invoice.Id,
        InvoiceNumber = invoice.InvoiceNumber,
        PurchaseOrderId = invoice.PurchaseOrderId,
        PurchaseOrderNumber = invoice.PurchaseOrder?.OrderNumber,
        VendorName = invoice.VendorName,
        UploadedByUserEmail = invoice.UploadedByUser?.Email,
        UploadedAt = invoice.CreatedAt,
        InvoiceDate = invoice.InvoiceDate
    };

    private static string? GetSafeStatusMessage(Invoice invoice)
    {
        if (invoice.Status == InvoiceStatus.Failed)
            return "Invoice processing failed. Contact support if the problem continues.";
        if (invoice.Status != InvoiceStatus.NeedsReview)
            return null;

        return invoice.ProcessingLogs
            .Where(log => log.EventType == "ValidationError")
            .OrderByDescending(log => log.CreatedAt)
            .Select(log => log.Message)
            .FirstOrDefault();
    }
}
