using SPIP.Application.Interfaces.Repositories;
using SPIP.Domain.Entities;
using SPIP.Application.DTOs.PurchaseOrder;

namespace SPIP.Application.Interfaces.Repositories;

public interface IPurchaseOrderRepository : IGenericRepository<PurchaseOrder>
{
    Task<(IReadOnlyList<PurchaseOrder> Items, int TotalCount)> GetPagedAsync(PurchaseOrderParameters parameters);
    Task<PurchaseOrder?> GetByOrderNumberAsync(string orderNumber);
    Task<PurchaseOrder?> GetWithItemsByIdAsync(int id);
}
