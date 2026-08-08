using Microsoft.EntityFrameworkCore;
using SPIP.Application.Interfaces.Repositories;
using SPIP.Domain.Entities;
using SPIP.Infrastructure.Persistence.Context;
using SPIP.Shared.Pagination;

namespace SPIP.Infrastructure.Repositories;

public class AIChatRepository : GenericRepository<AIChatSession>, IAIChatRepository
{
    public AIChatRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<AIChatSession?> GetWithMessagesByIdAsync(int sessionId, CancellationToken ct = default)
    {
        return await DbSet
            .Include(s => s.Messages.Where(m => !m.IsDeleted).OrderBy(m => m.CreatedAt))
            .FirstOrDefaultAsync(s => s.Id == sessionId, ct);
    }

    public async Task<(IReadOnlyList<AIChatSession> Items, int TotalCount)> GetUserSessionsPagedAsync(
        int userId, PaginationRequest parameters, CancellationToken ct = default)
    {
        var pageNumber = Math.Max(parameters.PageNumber, 1);
        var pageSize = Math.Clamp(parameters.PageSize, 1, 50);

        var query = DbSet
            .Where(s => s.UserId == userId && !s.IsArchived)
            .AsQueryable();

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .Include(s => s.Messages.Where(m => !m.IsDeleted).OrderByDescending(m => m.CreatedAt).Take(1))
            .OrderByDescending(s => s.UpdatedAt ?? s.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }
}
