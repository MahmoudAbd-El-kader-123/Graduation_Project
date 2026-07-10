using SPIP.Application.DTOs.User;
using SPIP.Application.Interfaces.Repositories;
using SPIP.Application.Interfaces.Services;
using SPIP.Domain.Entities;
using SPIP.Domain.Enums;
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

    public async Task<Result<IEnumerable<UserDto>>> GetAllAsync()
    {
        var users = await _userRepository.GetAllAsync();
        return Result<IEnumerable<UserDto>>.Success(users.Select(MapToDto));
    }

    public async Task<Result<UserDto>> CreateAsync(CreateUserDto dto)
    {
        var user = new User
        {
            FullName = dto.FullName,
            Email = dto.Email,
            PasswordHash = dto.Password, // Hashing handled in Infrastructure layer
            Role = Enum.TryParse<UserRole>(dto.Role, true, out var role) ? role : UserRole.Employee,
            IsActive = true
        };

        var created = await _userRepository.AddAsync(user);
        await _userRepository.SaveChangesAsync();

        return Result<UserDto>.Success(MapToDto(created));
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
