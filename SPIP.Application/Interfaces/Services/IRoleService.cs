using SPIP.Application.DTOs.Role;
using SPIP.Shared.Pagination;
using SPIP.Shared.Result;

namespace SPIP.Application.Interfaces.Services;

public interface IRoleService
{
    Task<Result<PagedResult<RoleDto>>> GetPagedAsync(RoleParameters parameters);
    Task<Result<RoleDto>> GetByIdAsync(Guid id);
    Task<Result<RoleDto>> CreateAsync(CreateRoleDto dto);
    Task<Result<RoleDto>> UpdateAsync(Guid id, CreateRoleDto dto);
    Task<Result<bool>> DeleteAsync(Guid id);
    Task<Result<bool>> AssignPermissionsAsync(Guid id, AssignPermissionsDto dto);
}
