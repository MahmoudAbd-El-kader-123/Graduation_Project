using SPIP.Domain.Entities;
using SPIP.Shared.Pagination;
using SPIP.Application.DTOs.User;

namespace SPIP.Application.Interfaces.Repositories;

public interface IUserRepository : IGenericRepository<User>
{
    Task<User?> GetByEmailAsync(string email);
    Task<(IReadOnlyList<User> Items, int TotalCount)> GetPagedAsync(UserParameters parameters);
    Task<User?> GetByIdentityIdAsync(Guid identityId);
}
