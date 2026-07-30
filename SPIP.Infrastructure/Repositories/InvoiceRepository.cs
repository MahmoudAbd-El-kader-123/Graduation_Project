using Microsoft.EntityFrameworkCore;
using SPIP.Application.DTOs.Invoice;
using SPIP.Application.Interfaces.Repositories;
using SPIP.Domain.Entities;
using SPIP.Domain.Enums;
using SPIP.Infrastructure.Persistence.Context;

namespace SPIP.Infrastructure.Repositories;

public class InvoiceRepository : GenericRepository<Invoice>, IInvoiceRepository
{
    public InvoiceRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<Invoice?> GetWithDetailsByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var invoice = await DbSet
            .Include(i => i.Items)
            .Include(i => i.Discrepancies)
            .Include(i => i.ProcessingLogs)
            .Include(i => i.UploadedFiles)
            .Include(i => i.UploadedByUser)
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);

        if (invoice is not null)
        {
            invoice.ProcessingLogs = invoice.ProcessingLogs
                .OrderBy(l => l.CreatedAt)
                .ToList();
        }

        return invoice;
    }

    public async Task<(IReadOnlyList<Invoice> Items, int TotalCount)> GetPagedAsync(InvoiceListParameters parameters, CancellationToken cancellationToken = default)
    {
        var pageNumber = Math.Max(parameters.PageNumber, 1);
        var pageSize = Math.Clamp(parameters.PageSize, 1, 50);
        var query = DbSet
            .Include(i => i.UploadedByUser)
            .Include(i => i.ProcessingLogs)
            .Include(i => i.PurchaseOrder)
            .Include(i => i.Discrepancies)
            .AsQueryable();

        query = ApplySearch(query, parameters.SearchTerm);
        query = ApplyFilters(query, parameters);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(i => i.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    private static IQueryable<Invoice> ApplySearch(
        IQueryable<Invoice> query,
        string? searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return query;

        var normalizedSearchTerm = searchTerm.ToLower();
        return query.Where(invoice =>
            invoice.InvoiceNumber.ToLower().Contains(normalizedSearchTerm) ||
            invoice.VendorName.ToLower().Contains(normalizedSearchTerm) ||
            (invoice.PurchaseOrder != null &&
             invoice.PurchaseOrder.OrderNumber.ToLower().Contains(normalizedSearchTerm)));
    }

    private static IQueryable<Invoice> ApplyFilters(
        IQueryable<Invoice> query,
        InvoiceListParameters parameters)
    {
        if (!string.IsNullOrWhiteSpace(parameters.Status) &&
            Enum.TryParse<InvoiceStatus>(parameters.Status, true, out var status))
            query = query.Where(invoice => invoice.Status == status);

        if (parameters.VendorId.HasValue)
            query = query.Where(invoice => invoice.VendorId == parameters.VendorId.Value);

        if (parameters.PurchaseOrderId.HasValue)
            query = query.Where(invoice => invoice.PurchaseOrderId == parameters.PurchaseOrderId.Value);

        return ApplyDiscrepancyFilter(query, parameters.HasDiscrepancies);
    }

    private static IQueryable<Invoice> ApplyDiscrepancyFilter(
        IQueryable<Invoice> query,
        bool? hasDiscrepancies)
    {
        if (!hasDiscrepancies.HasValue)
            return query;

        return hasDiscrepancies.Value
            ? query.Where(invoice => invoice.Discrepancies.Any())
            : query.Where(invoice => !invoice.Discrepancies.Any());
    }

    public async Task<IReadOnlyList<Invoice>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(i => i.UploadedByUserId == userId)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}
