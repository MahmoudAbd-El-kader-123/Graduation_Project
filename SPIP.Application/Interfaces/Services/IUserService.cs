using SPIP.Application.DTOs.User;
using SPIP.Shared.Pagination;
using SPIP.Shared.Result;

namespace SPIP.Application.Interfaces.Services;

public interface IUserService
{
    Task<Result<UserDto>> GetByIdAsync(int id);
    Task<Result<PagedResult<UserDto>>> GetPagedAsync(UserParameters parameters);
    Task<Result<UserDto>> UpdateAsync(int id, UpdateUserDto dto);
    Task<Result<bool>> DeleteAsync(int id);
    Task<Result<bool>> ToggleActiveStatusAsync(int id);
}
