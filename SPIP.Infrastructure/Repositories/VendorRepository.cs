using Microsoft.EntityFrameworkCore;
using SPIP.Application.DTOs.Vendor;
using SPIP.Application.Interfaces.Repositories;
using SPIP.Domain.Entities;
using SPIP.Infrastructure.Persistence.Context;

namespace SPIP.Infrastructure.Repositories;

public class VendorRepository : GenericRepository<Vendor>, IVendorRepository
{
    public VendorRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<(IReadOnlyList<Vendor> Items, int TotalCount)> GetPagedAsync(VendorParameters p)
    {
        var query = DbSet.AsQueryable();

        if (!string.IsNullOrWhiteSpace(p.SearchTerm))
        {
            var search = p.SearchTerm.ToLower();
            query = query.Where(v => v.Name.ToLower().Contains(search) || 
                                     (v.ErpId != null && v.ErpId.ToLower().Contains(search)) ||
                                     (v.TaxRegistrationNumber != null && v.TaxRegistrationNumber.ToLower().Contains(search)));
        }

        if (p.IsApproved.HasValue)
        {
            query = query.Where(v => v.IsApproved == p.IsApproved.Value);
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(v => v.Id)
            .Skip((p.PageNumber - 1) * p.PageSize)
            .Take(p.PageSize)
            .ToListAsync();

        return (items, totalCount);
    }
}
