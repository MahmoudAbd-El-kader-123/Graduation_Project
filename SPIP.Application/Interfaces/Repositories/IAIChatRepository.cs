using SPIP.Domain.Entities;
using SPIP.Shared.Pagination;

namespace SPIP.Application.Interfaces.Repositories;

/// <summary>
/// Repository for AIChat entities.
/// All session-fetch methods are authorization-aware: they filter by both sessionId AND userId
/// in the WHERE clause to prevent IDOR at the database query level.
/// </summary>
public interface IAIChatRepository
{
    // ── Session queries (auth-aware) ──────────────────────────────────────────

    /// <summary>
    /// Fetches a session belonging to the specified user. Returns null if not found OR not owned.
    /// Callers must treat null uniformly as "not found" — do not distinguish "not yours" vs "doesn't exist".
    /// </summary>
    Task<AIChatSession?> GetSessionForUserAsync(Guid sessionId, int userId, CancellationToken ct = default);

    /// <summary>
    /// Fetches a session with all non-deleted messages, scoped to the owning user.
    /// Returns null if not found OR not owned.
    /// </summary>
    Task<AIChatSession?> GetSessionWithMessagesForUserAsync(Guid sessionId, int userId, CancellationToken ct = default);

    /// <summary>
    /// Returns a paginated, ordered list of non-archived sessions for a user.
    /// </summary>
    Task<(IReadOnlyList<AIChatSession> Items, int TotalCount)> GetUserSessionsPagedAsync(
        int userId, PaginationRequest parameters, CancellationToken ct = default);

    // ── Write operations ──────────────────────────────────────────────────────

    Task<AIChatSession> AddAsync(AIChatSession session, CancellationToken ct = default);
    Task UpdateAsync(AIChatSession session);
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
