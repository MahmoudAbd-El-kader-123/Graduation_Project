using Microsoft.EntityFrameworkCore;
using SPIP.Application.DTOs.Product;
using SPIP.Application.Interfaces.Repositories;
using SPIP.Domain.Entities;
using SPIP.Infrastructure.Persistence.Context;

namespace SPIP.Infrastructure.Repositories;

public class ProductRepository : GenericRepository<Product>, IProductRepository
{
    public ProductRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<(IReadOnlyList<Product> Items, int TotalCount)> GetPagedAsync(ProductParameters p)
    {
        var query = DbSet.Include(x => x.Vendor).AsQueryable();

        if (!string.IsNullOrWhiteSpace(p.SearchTerm))
        {
            var search = p.SearchTerm.ToLower();
            query = query.Where(v => v.Name.ToLower().Contains(search) || 
                                     (v.ErpId != null && v.ErpId.ToLower().Contains(search)) ||
                                     (v.SkuSupplier != null && v.SkuSupplier.ToLower().Contains(search)) ||
                                     (v.Barcode != null && v.Barcode.ToLower().Contains(search)));
        }

        if (p.VendorId.HasValue)
        {
            query = query.Where(v => v.VendorId == p.VendorId.Value);
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(v => v.Id)
            .Skip((p.PageNumber - 1) * p.PageSize)
            .Take(p.PageSize)
            .ToListAsync();

        return (items, totalCount);
    }
    
    public override async Task<Product?> GetByIdAsync(int id)
    {
        return await DbSet.Include(x => x.Vendor).FirstOrDefaultAsync(x => x.Id == id);
    }
}
