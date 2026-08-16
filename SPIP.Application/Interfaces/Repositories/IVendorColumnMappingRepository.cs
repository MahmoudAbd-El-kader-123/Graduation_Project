using SPIP.Application.Interfaces.Repositories;
using SPIP.Domain.Entities;

namespace SPIP.Application.Interfaces.Repositories;

public interface IVendorColumnMappingRepository : IGenericRepository<VendorColumnMapping>
{
    Task<IReadOnlyList<VendorColumnMapping>> GetByVendorIdAsync(int vendorId);
    Task<VendorColumnMapping?> GetBySystemFieldAsync(int vendorId, string systemField);
}
