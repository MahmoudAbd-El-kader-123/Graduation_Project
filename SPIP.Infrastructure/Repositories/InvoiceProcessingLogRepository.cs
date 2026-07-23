using Microsoft.EntityFrameworkCore;
using SPIP.Application.Interfaces.Repositories;
using SPIP.Domain.Entities;
using SPIP.Infrastructure.Persistence.Context;

namespace SPIP.Infrastructure.Repositories;

public class InvoiceProcessingLogRepository : GenericRepository<InvoiceProcessingLog>, IInvoiceProcessingLogRepository
{
    public InvoiceProcessingLogRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<InvoiceProcessingLog>> GetByInvoiceIdAsync(int invoiceId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(l => l.InvoiceId == invoiceId)
            .OrderBy(l => l.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}
