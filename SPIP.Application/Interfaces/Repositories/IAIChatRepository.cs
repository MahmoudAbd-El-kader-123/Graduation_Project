using SPIP.Domain.Entities;
using SPIP.Shared.Pagination;

namespace SPIP.Application.Interfaces.Repositories;

public interface IAIChatRepository : IGenericRepository<AIChatSession>
{
    Task<AIChatSession?> GetWithMessagesByIdAsync(int sessionId, CancellationToken ct = default);
    Task<(IReadOnlyList<AIChatSession> Items, int TotalCount)> GetUserSessionsPagedAsync(
        int userId, PaginationRequest parameters, CancellationToken ct = default);
}
