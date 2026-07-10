using SPIP.Application.DTOs.User;
using SPIP.Shared.Result;

namespace SPIP.Application.Interfaces.Services;

public interface IUserService
{
    Task<Result<UserDto>> GetByIdAsync(int id);
    Task<Result<IEnumerable<UserDto>>> GetAllAsync();
    Task<Result<UserDto>> CreateAsync(CreateUserDto dto);
    Task<Result<UserDto>> UpdateAsync(int id, UpdateUserDto dto);
    Task<Result<bool>> DeleteAsync(int id);
}
