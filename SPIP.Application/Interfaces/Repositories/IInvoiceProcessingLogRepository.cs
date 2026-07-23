using SPIP.Domain.Entities;

namespace SPIP.Application.Interfaces.Repositories;

public interface IInvoiceProcessingLogRepository : IGenericRepository<InvoiceProcessingLog>
{
    Task<IReadOnlyList<InvoiceProcessingLog>> GetByInvoiceIdAsync(int invoiceId, CancellationToken cancellationToken = default);
}
