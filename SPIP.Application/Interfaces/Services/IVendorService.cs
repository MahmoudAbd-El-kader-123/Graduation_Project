using SPIP.Application.DTOs.Vendor;
using SPIP.Shared.Pagination;
using SPIP.Shared.Result;

namespace SPIP.Application.Interfaces.Services;

public interface IVendorService
{
    Task<Result<PagedResult<VendorDto>>> GetPagedAsync(VendorParameters parameters);
    Task<Result<VendorDto>> GetByIdAsync(int id);
    Task<Result<VendorDto>> CreateAsync(CreateVendorDto dto);
    Task<Result<VendorDto>> UpdateAsync(int id, CreateVendorDto dto);
    Task<Result<bool>> DeleteAsync(int id);
    Task<Result<bool>> ToggleApprovalStatusAsync(int id);
}
