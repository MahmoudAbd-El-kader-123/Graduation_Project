using SPIP.Application.DTOs.AIChat;
using SPIP.Shared.Pagination;
using SPIP.Shared.Result;

namespace SPIP.Application.Interfaces.Services;

public interface IAIChatService
{
    Task<Result<ChatSessionListDto>> CreateSessionAsync(CreateChatSessionDto dto, CancellationToken ct = default);
    Task<Result<PagedResult<ChatSessionListDto>>> GetSessionsAsync(PaginationRequest parameters, CancellationToken ct = default);
    Task<Result<ChatSessionDetailDto>> GetSessionByIdAsync(Guid sessionId, CancellationToken ct = default);
    Task<Result<ChatResponseDto>> SendMessageAsync(Guid sessionId, SendChatMessageDto dto, CancellationToken ct = default);
    Task<Result<bool>> DeleteSessionAsync(Guid sessionId, CancellationToken ct = default);
    Task<Result<bool>> UpdateSessionTitleAsync(Guid sessionId, UpdateSessionTitleDto dto, CancellationToken ct = default);
}
