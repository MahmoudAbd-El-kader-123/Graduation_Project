using Microsoft.EntityFrameworkCore;
using SPIP.Application.DTOs.PurchaseOrder;
using SPIP.Application.Interfaces.Repositories;
using SPIP.Domain.Entities;
using SPIP.Infrastructure.Persistence.Context;

namespace SPIP.Infrastructure.Repositories;

public class PurchaseOrderRepository : GenericRepository<PurchaseOrder>, IPurchaseOrderRepository
{
    public PurchaseOrderRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<(IReadOnlyList<PurchaseOrder> Items, int TotalCount)> GetPagedAsync(PurchaseOrderParameters p)
    {
        var query = DbSet.Include(x => x.Vendor).Include(x => x.RequestedByUser).AsQueryable();

        if (!string.IsNullOrWhiteSpace(p.OrderNumber))
        {
            var search = p.OrderNumber.ToLower();
            query = query.Where(v => v.OrderNumber.ToLower().Contains(search));
        }

        if (p.VendorId.HasValue)
        {
            query = query.Where(v => v.VendorId == p.VendorId.Value);
        }

        if (p.Status.HasValue)
        {
            query = query.Where(v => v.Status == p.Status.Value);
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(v => v.Id)
            .Skip((p.PageNumber - 1) * p.PageSize)
            .Take(p.PageSize)
            .ToListAsync();

        return (items, totalCount);
    }
    
    public async Task<PurchaseOrder?> GetByOrderNumberAsync(string orderNumber)
    {
        return await DbSet.FirstOrDefaultAsync(x => x.OrderNumber == orderNumber);
    }

    public async Task<PurchaseOrder?> GetWithItemsByIdAsync(int id)
    {
        return await DbSet
            .Include(x => x.Vendor)
            .Include(x => x.RequestedByUser)
            .Include(x => x.Items)
                .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(x => x.Id == id);
    }
}
