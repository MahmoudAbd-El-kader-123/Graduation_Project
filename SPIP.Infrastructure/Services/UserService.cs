using Microsoft.AspNetCore.Identity;
using SPIP.Application.DTOs.User;
using SPIP.Application.Interfaces.Repositories;
using SPIP.Application.Interfaces.Services;
using SPIP.Domain.Entities;
using SPIP.Infrastructure.Identity;
using SPIP.Shared.Pagination;
using SPIP.Shared.Result;

namespace SPIP.Infrastructure.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;

    public UserService(
        IUserRepository userRepository,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole<Guid>> roleManager)
    {
        _userRepository = userRepository;
        _userManager = userManager;
        _roleManager = roleManager;
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
        var domainUser = await _userRepository.GetByIdAsync(id);
        if (domainUser is null)
            return Result<UserDto>.Failure("User not found.");

        var appUser = await _userManager.FindByIdAsync(domainUser.IdentityId.ToString());
        if (appUser == null)
            return Result<UserDto>.Failure("Identity User not found.");

        var newRole = await _roleManager.FindByIdAsync(dto.RoleId.ToString());
        if (newRole == null)
            return Result<UserDto>.Failure("Role not found.");

        // Update Identity Roles
        var currentRoles = await _userManager.GetRolesAsync(appUser);
        await _userManager.RemoveFromRolesAsync(appUser, currentRoles);
        await _userManager.AddToRoleAsync(appUser, newRole.Name!);

        // Update Domain User
        domainUser.FullName = dto.FullName;
        domainUser.IsActive = dto.IsActive;
        domainUser.RoleId = newRole.Id;
        domainUser.RoleName = newRole.Name!;

        await _userRepository.UpdateAsync(domainUser);
        await _userRepository.SaveChangesAsync();

        // Update Identity User basic details if needed
        appUser.FullName = dto.FullName;
        appUser.IsActive = dto.IsActive;
        await _userManager.UpdateAsync(appUser);

        return Result<UserDto>.Success(MapToDto(domainUser));
    }

    public async Task<Result<bool>> ToggleActiveStatusAsync(int id)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user is null)
            return Result<bool>.Failure("User not found.");

        user.IsActive = !user.IsActive;
        await _userRepository.UpdateAsync(user);
        await _userRepository.SaveChangesAsync();
        
        var appUser = await _userManager.FindByIdAsync(user.IdentityId.ToString());
        if (appUser != null)
        {
            appUser.IsActive = user.IsActive;
            await _userManager.UpdateAsync(appUser);
        }

        return Result<bool>.Success(user.IsActive);
    }

    public async Task<Result<bool>> DeleteAsync(int id)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user is null)
            return Result<bool>.Failure("User not found.");

        await _userRepository.DeleteAsync(user);
        await _userRepository.SaveChangesAsync();
        
        var appUser = await _userManager.FindByIdAsync(user.IdentityId.ToString());
        if (appUser != null)
        {
            await _userManager.DeleteAsync(appUser);
        }

        return Result<bool>.Success(true);
    }

    private static UserDto MapToDto(User user) => new()
    {
        Id = user.Id,
        FullName = user.FullName,
        Email = user.Email,
        RoleId = user.RoleId,
        Role = user.RoleName,
        IsActive = user.IsActive
    };
}
