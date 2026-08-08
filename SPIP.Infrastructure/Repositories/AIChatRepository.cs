using Microsoft.EntityFrameworkCore;
using SPIP.Application.Interfaces.Repositories;
using SPIP.Domain.Entities;
using SPIP.Infrastructure.Persistence.Context;
using SPIP.Shared.Pagination;

namespace SPIP.Infrastructure.Repositories;

/// <summary>
/// All fetch methods are authorization-aware: ownership is enforced at the SQL WHERE clause level,
/// not in application code after fetching. This prevents IDOR at the architectural layer.
/// </summary>
public class AIChatRepository : IAIChatRepository
{
    private readonly ApplicationDbContext _context;

    public AIChatRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    // ── Auth-aware queries ────────────────────────────────────────────────────

    /// <inheritdoc />
    public async Task<AIChatSession?> GetSessionForUserAsync(
        Guid sessionId, int userId, CancellationToken ct = default)
    {
        return await _context.AIChatSessions
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == userId, ct);
    }

    /// <inheritdoc />
    public async Task<AIChatSession?> GetSessionWithMessagesForUserAsync(
        Guid sessionId, int userId, CancellationToken ct = default)
    {
        return await _context.AIChatSessions
            // Include only non-deleted messages, ordered chronologically for conversation display.
            .Include(s => s.Messages.Where(m => !m.IsDeleted).OrderBy(m => m.CreatedAt))
            .FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == userId, ct);
    }

    /// <inheritdoc />
    public async Task<(IReadOnlyList<AIChatSession> Items, int TotalCount)> GetUserSessionsPagedAsync(
        int userId, PaginationRequest parameters, CancellationToken ct = default)
    {
        var pageNumber = Math.Max(parameters.PageNumber, 1);
        var pageSize = Math.Clamp(parameters.PageSize, 1, 50);

        var query = _context.AIChatSessions
            .AsNoTracking()
            .Where(s => s.UserId == userId && !s.IsArchived);

        var totalCount = await query.CountAsync(ct);

        var items = await query
            // Load the single most-recent non-deleted message for LastMessage preview.
            .Include(s => s.Messages
                .Where(m => !m.IsDeleted)
                .OrderByDescending(m => m.CreatedAt)
                .Take(1))
            .OrderByDescending(s => s.UpdatedAt ?? s.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return ((IReadOnlyList<AIChatSession>)items, totalCount);
    }

    // ── Write operations ──────────────────────────────────────────────────────

    public async Task<AIChatSession> AddAsync(AIChatSession session, CancellationToken ct = default)
    {
        await _context.AIChatSessions.AddAsync(session, ct);
        return session;
    }

    public Task UpdateAsync(AIChatSession session)
    {
        _context.AIChatSessions.Update(session);
        return Task.CompletedTask;
    }

    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
        => await _context.SaveChangesAsync(ct);
}
