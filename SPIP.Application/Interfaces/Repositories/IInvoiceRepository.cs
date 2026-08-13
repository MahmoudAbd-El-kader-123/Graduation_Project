using SPIP.Application.DTOs.Invoice;
using SPIP.Domain.Entities;

namespace SPIP.Application.Interfaces.Repositories;

public interface IInvoiceRepository : IGenericRepository<Invoice>
{
    Task<Invoice?> GetWithDetailsByIdAsync(int id, CancellationToken cancellationToken = default);
    void ClearReconciliationResults(Invoice invoice);
    Task<(IReadOnlyList<Invoice> Items, int TotalCount)> GetPagedAsync(InvoiceListParameters parameters, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Invoice>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default);
}
