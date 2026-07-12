using SPIP.Application.DTOs.User;
using SPIP.Application.Interfaces.Repositories;
using SPIP.Application.Interfaces.Services;
using SPIP.Domain.Entities;
using SPIP.Domain.Enums;
using SPIP.Shared.Pagination;
using SPIP.Shared.Result;

namespace SPIP.Application.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;

    public UserService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<Result<UserDto>> GetByIdAsync(int id)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user is null)
            return Result<UserDto>.Failure("User not found.");

        return Result<UserDto>.Success(MapToDto(user));
    }

    public async Task<Result<PagedResult<UserDto>>> GetPagedAsync(UserParameters parameters)
    {
        var (items, totalCount) = await _userRepository.GetPagedAsync(parameters);
        var pagedResult = new PagedResult<UserDto>
        {
            Items = items.Select(MapToDto).ToList(),
            PageNumber = parameters.PageNumber,
            PageSize = parameters.PageSize,
            TotalCount = totalCount
        };
        return Result<PagedResult<UserDto>>.Success(pagedResult);
    }



    public async Task<Result<UserDto>> UpdateAsync(int id, UpdateUserDto dto)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user is null)
            return Result<UserDto>.Failure("User not found.");

        user.FullName = dto.FullName;
        user.IsActive = dto.IsActive;
        if (Enum.TryParse<UserRole>(dto.Role, true, out var role))
            user.Role = role;

        await _userRepository.UpdateAsync(user);
        await _userRepository.SaveChangesAsync();

        return Result<UserDto>.Success(MapToDto(user));
    }

    public async Task<Result<bool>> ToggleActiveStatusAsync(int id)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user is null)
            return Result<bool>.Failure("User not found.");

        user.IsActive = !user.IsActive;
        await _userRepository.UpdateAsync(user);
        await _userRepository.SaveChangesAsync();

        return Result<bool>.Success(user.IsActive);
    }

    public async Task<Result<bool>> DeleteAsync(int id)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user is null)
            return Result<bool>.Failure("User not found.");

        await _userRepository.DeleteAsync(user);
        await _userRepository.SaveChangesAsync();

        return Result<bool>.Success(true);
    }

    private static UserDto MapToDto(User user) => new()
    {
        Id = user.Id,
        FullName = user.FullName,
        Email = user.Email,
        Role = user.Role.ToString(),
        IsActive = user.IsActive
    };
}
