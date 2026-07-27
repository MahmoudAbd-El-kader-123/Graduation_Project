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
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(parameters.Status) &&
            Enum.TryParse<InvoiceStatus>(parameters.Status, true, out var status))
        {
            query = query.Where(i => i.Status == status);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(i => i.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<IReadOnlyList<Invoice>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(i => i.UploadedByUserId == userId)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}
