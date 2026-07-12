using SPIP.Application.Interfaces.Repositories;
using SPIP.Domain.Entities;
using SPIP.Application.DTOs.Vendor;

namespace SPIP.Application.Interfaces.Repositories;

public interface IVendorRepository : IGenericRepository<Vendor>
{
    Task<(IReadOnlyList<Vendor> Items, int TotalCount)> GetPagedAsync(VendorParameters parameters);
}
