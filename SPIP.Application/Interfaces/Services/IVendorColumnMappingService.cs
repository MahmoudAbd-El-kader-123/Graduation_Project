using SPIP.Application.DTOs.VendorColumnMapping;
using SPIP.Shared.Result;

namespace SPIP.Application.Interfaces.Services;

public interface IVendorColumnMappingService
{
    Task<Result<IReadOnlyList<VendorColumnMappingDto>>> GetByVendorIdAsync(int vendorId);
    Task<Result<VendorColumnMappingDto>> CreateOrUpdateAsync(CreateVendorColumnMappingDto dto);
    Task<Result<bool>> DeleteAsync(int id);
}
