using Microsoft.EntityFrameworkCore;
using SPIP.Application.DTOs.AuditLog;
using SPIP.Application.Interfaces.Services;
using SPIP.Infrastructure.Persistence.Context;
using SPIP.Shared.Pagination;
using SPIP.Shared.Result;

namespace SPIP.Infrastructure.Services;

public class AuditLogService : IAuditLogService
{
    private readonly ApplicationDbContext _context;

    public AuditLogService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<PagedResult<AuditLogDto>>> GetPagedAsync(AuditLogParameters p)
    {
        var query = _context.Set<Domain.Entities.AuditLog>()
            .Include(a => a.User)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(p.EntityName))
        {
            query = query.Where(a => a.EntityName == p.EntityName);
        }

        if (!string.IsNullOrWhiteSpace(p.Action))
        {
            query = query.Where(a => a.Action == p.Action);
        }

        if (p.UserId.HasValue)
        {
            query = query.Where(a => a.UserId == p.UserId.Value);
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip((p.PageNumber - 1) * p.PageSize)
            .Take(p.PageSize)
            .Select(a => new AuditLogDto
            {
                Id = a.Id,
                UserId = a.UserId,
                UserName = a.User != null ? a.User.FullName : null,
                Action = a.Action,
                EntityName = a.EntityName,
                EntityId = a.EntityId,
                Details = a.Details,
                CreatedAt = a.CreatedAt
            })
            .ToListAsync();

        return Result<PagedResult<AuditLogDto>>.Success(new PagedResult<AuditLogDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = p.PageNumber,
            PageSize = p.PageSize
        });
    }

    public async Task<Result<AuditLogDto>> GetByIdAsync(int id)
    {
        var a = await _context.Set<Domain.Entities.AuditLog>()
            .Include(x => x.User)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id);

        if (a == null) return Result<AuditLogDto>.Failure("Audit log not found.");

        return Result<AuditLogDto>.Success(new AuditLogDto
        {
            Id = a.Id,
            UserId = a.UserId,
            UserName = a.User != null ? a.User.FullName : null,
            Action = a.Action,
            EntityName = a.EntityName,
            EntityId = a.EntityId,
            Details = a.Details,
            CreatedAt = a.CreatedAt
        });
    }
}
