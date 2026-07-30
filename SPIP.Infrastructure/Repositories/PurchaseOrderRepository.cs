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

    public async Task<(IReadOnlyList<PurchaseOrder> Items, int TotalCount)> GetPagedAsync(PurchaseOrderParameters parameters)
    {
        var query = DbSet.Include(x => x.Vendor).Include(x => x.RequestedByUser).AsQueryable();
        query = ApplySearch(query, parameters);
        query = ApplyFilters(query, parameters);

        var totalCount = await query.CountAsync();

        var purchaseOrders = await query
            .OrderByDescending(v => v.Id)
            .Skip((parameters.PageNumber - 1) * parameters.PageSize)
            .Take(parameters.PageSize)
            .ToListAsync();

        return (purchaseOrders, totalCount);
    }

    private static IQueryable<PurchaseOrder> ApplySearch(
        IQueryable<PurchaseOrder> query,
        PurchaseOrderParameters parameters)
    {
        if (!string.IsNullOrWhiteSpace(parameters.SearchTerm))
        {
            var searchTerm = parameters.SearchTerm.ToLower();
            query = query.Where(po =>
                po.OrderNumber.ToLower().Contains(searchTerm) ||
                (po.Vendor != null && po.Vendor.Name.ToLower().Contains(searchTerm)));
        }

        if (string.IsNullOrWhiteSpace(parameters.OrderNumber))
            return query;

        var orderNumber = parameters.OrderNumber.ToLower();
        return query.Where(po => po.OrderNumber.ToLower().Contains(orderNumber));
    }

    private static IQueryable<PurchaseOrder> ApplyFilters(
        IQueryable<PurchaseOrder> query,
        PurchaseOrderParameters parameters)
    {
        if (parameters.VendorId.HasValue)
            query = query.Where(po => po.VendorId == parameters.VendorId.Value);

        if (parameters.Status.HasValue)
            query = query.Where(po => po.Status == parameters.Status.Value);

        return query;
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
