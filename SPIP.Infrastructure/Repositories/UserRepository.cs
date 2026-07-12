using Microsoft.EntityFrameworkCore;
using SPIP.Application.DTOs.User;
using SPIP.Application.Interfaces.Repositories;
using SPIP.Domain.Entities;
using SPIP.Infrastructure.Persistence.Context;

namespace SPIP.Infrastructure.Repositories;

public class UserRepository : GenericRepository<User>, IUserRepository
{
    public UserRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<User?> GetByEmailAsync(string email) =>
        await DbSet.FirstOrDefaultAsync(u => u.Email == email);

    public async Task<(IReadOnlyList<User> Items, int TotalCount)> GetPagedAsync(UserParameters p)
    {
        var query = DbSet.AsQueryable();

        if (!string.IsNullOrWhiteSpace(p.SearchTerm))
        {
            var search = p.SearchTerm.ToLower();
            query = query.Where(u => u.FullName.ToLower().Contains(search) || u.Email.ToLower().Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(p.Role) && Enum.TryParse<SPIP.Domain.Enums.UserRole>(p.Role, true, out var parsedRole))
        {
            query = query.Where(u => u.Role == parsedRole);
        }

        if (p.IsActive.HasValue)
        {
            query = query.Where(u => u.IsActive == p.IsActive.Value);
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(u => u.Id)
            .Skip((p.PageNumber - 1) * p.PageSize)
            .Take(p.PageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<User?> GetByIdentityIdAsync(Guid identityId)
    {
        return await DbSet.FirstOrDefaultAsync(u => u.IdentityId == identityId);
    }
}
