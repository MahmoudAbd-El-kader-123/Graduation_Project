using Microsoft.EntityFrameworkCore;
using SPIP.Application.DTOs.ReconciliationReport;
using SPIP.Application.Interfaces.Repositories;
using SPIP.Domain.Entities;
using SPIP.Domain.Enums;
using SPIP.Infrastructure.Persistence.Context;

namespace SPIP.Infrastructure.Repositories;

public sealed class ReconciliationReportRepository : IReconciliationReportRepository
{
    private readonly ApplicationDbContext _context;

    public ReconciliationReportRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<(IReadOnlyList<ReconciliationReportSummaryDto> Items, int TotalCount)> GetPageAsync(
        ReconciliationReportParameters parameters,
        CancellationToken cancellationToken = default)
    {
        var scopedInvoices = ApplyReportSecurityScope(_context.Invoices.AsNoTracking());
        var filteredInvoices = ApplyFilters(scopedInvoices, parameters);
        var totalCount = await filteredInvoices.CountAsync(cancellationToken);
        var orderedInvoices = ApplyOrdering(filteredInvoices, parameters);

        var summaries = await orderedInvoices
            .Skip((parameters.PageNumber - 1) * parameters.PageSize)
            .Take(parameters.PageSize)
            .Select(invoice => new ReconciliationReportSummaryDto
            {
                InvoiceId = invoice.Id,
                InvoiceNumber = invoice.InvoiceNumber,
                PurchaseOrderId = invoice.PurchaseOrderId,
                PurchaseOrderNumber = invoice.PurchaseOrder != null ? invoice.PurchaseOrder.OrderNumber : null,
                VendorName = invoice.VendorName,
                UploadedByUserEmail = invoice.UploadedByUser != null ? invoice.UploadedByUser.Email : null,
                Status = invoice.Status.ToString(),
                DiscrepancyCount = invoice.Discrepancies.Count,
                HasDiscrepancies = invoice.Discrepancies.Any(),
                DifferentItemCount = invoice.ReconciliationItems.Count(row => row.Status == ReconciliationItemStatus.Different),
                MissingFromInvoiceCount = invoice.ReconciliationItems.Count(row => row.Status == ReconciliationItemStatus.MissingFromInvoice),
                MissingFromPurchaseOrderCount = invoice.ReconciliationItems.Count(row => row.Status == ReconciliationItemStatus.MissingFromPurchaseOrder),
                UploadedAt = invoice.CreatedAt,
                StatusMessage = invoice.Status == InvoiceStatus.NeedsReview
                    ? invoice.ProcessingLogs
                        .Where(log => log.EventType == "ValidationError")
                        .OrderByDescending(log => log.CreatedAt)
                        .Select(log => log.Message)
                        .FirstOrDefault()
                    : invoice.Status == InvoiceStatus.Failed
                        ? "Invoice processing failed. Contact support if the problem continues."
                        : null
            })
            .ToListAsync(cancellationToken);

        return (summaries, totalCount);
    }

    public Task<Invoice?> GetDetailAsync(int invoiceId, CancellationToken cancellationToken = default) =>
        ApplyReportSecurityScope(_context.Invoices.AsNoTracking())
            .AsSplitQuery()
            .Include(invoice => invoice.PurchaseOrder)
            .Include(invoice => invoice.UploadedByUser)
            .Include(invoice => invoice.Discrepancies)
                .ThenInclude(discrepancy => discrepancy.ReconciliationItem)
            .Include(invoice => invoice.ReconciliationItems)
                .ThenInclude(row => row.Discrepancies)
            .Include(invoice => invoice.ProcessingLogs)
            .FirstOrDefaultAsync(invoice => invoice.Id == invoiceId, cancellationToken);

    // Future tenant scope belongs here, before filters, projection, sorting, and pagination.
    private static IQueryable<Invoice> ApplyReportSecurityScope(IQueryable<Invoice> invoices) => invoices;

    private static IQueryable<Invoice> ApplyFilters(
        IQueryable<Invoice> invoices,
        ReconciliationReportParameters parameters)
    {
        if (!string.IsNullOrWhiteSpace(parameters.SearchTerm))
        {
            var searchTerm = parameters.SearchTerm.Trim();
            invoices = invoices.Where(invoice =>
                invoice.InvoiceNumber.Contains(searchTerm) ||
                invoice.VendorName.Contains(searchTerm) ||
                (invoice.PurchaseOrder != null && invoice.PurchaseOrder.OrderNumber.Contains(searchTerm)) ||
                (invoice.UploadedByUser != null && invoice.UploadedByUser.Email.Contains(searchTerm)));
        }

        if (!string.IsNullOrWhiteSpace(parameters.Status) &&
            Enum.TryParse<InvoiceStatus>(parameters.Status, true, out var status))
            invoices = invoices.Where(invoice => invoice.Status == status);

        if (parameters.HasDiscrepancies.HasValue)
            invoices = parameters.HasDiscrepancies.Value
                ? invoices.Where(invoice => invoice.Discrepancies.Any())
                : invoices.Where(invoice => !invoice.Discrepancies.Any());

        if (parameters.FromDate.HasValue)
        {
            var fromDate = parameters.FromDate.Value.Date;
            invoices = invoices.Where(invoice => invoice.CreatedAt >= fromDate);
        }
        if (parameters.ToDate.HasValue)
        {
            var exclusiveToDate = parameters.ToDate.Value.Date.AddDays(1);
            invoices = invoices.Where(invoice => invoice.CreatedAt < exclusiveToDate);
        }

        return invoices;
    }

    private static IOrderedQueryable<Invoice> ApplyOrdering(
        IQueryable<Invoice> invoices,
        ReconciliationReportParameters parameters)
    {
        var ascending = string.Equals(parameters.SortDirection, "asc", StringComparison.OrdinalIgnoreCase);
        if (string.Equals(parameters.SortBy, "discrepancyCount", StringComparison.OrdinalIgnoreCase))
        {
            return ascending
                ? invoices.OrderBy(invoice => invoice.Discrepancies.Count).ThenBy(invoice => invoice.Id)
                : invoices.OrderByDescending(invoice => invoice.Discrepancies.Count).ThenByDescending(invoice => invoice.Id);
        }

        return ascending
            ? invoices.OrderBy(invoice => invoice.CreatedAt).ThenBy(invoice => invoice.Id)
            : invoices.OrderByDescending(invoice => invoice.CreatedAt).ThenByDescending(invoice => invoice.Id);
    }
}
