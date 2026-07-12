using Microsoft.EntityFrameworkCore;
using SPIP.Application.Interfaces.Repositories;
using SPIP.Domain.Entities;
using SPIP.Infrastructure.Persistence.Context;

namespace SPIP.Infrastructure.Repositories;

public class VendorColumnMappingRepository : GenericRepository<VendorColumnMapping>, IVendorColumnMappingRepository
{
    public VendorColumnMappingRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<VendorColumnMapping>> GetByVendorIdAsync(int vendorId)
    {
        return await DbSet
            .Where(m => m.VendorId == vendorId)
            .ToListAsync();
    }

    public async Task<VendorColumnMapping?> GetBySystemFieldAsync(int vendorId, string systemField)
    {
        return await DbSet
            .FirstOrDefaultAsync(m => m.VendorId == vendorId && m.SystemField == systemField);
    }
}
