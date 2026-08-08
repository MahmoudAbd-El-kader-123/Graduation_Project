using SPIP.Application.DTOs.AIChat;
using SPIP.Shared.Pagination;
using SPIP.Shared.Result;

namespace SPIP.Application.Interfaces.Services;

public interface IAIChatService
{
    Task<Result<ChatSessionListDto>> CreateSessionAsync(CreateChatSessionDto dto);
    Task<Result<PagedResult<ChatSessionListDto>>> GetSessionsAsync(PaginationRequest parameters);
    Task<Result<ChatSessionDetailDto>> GetSessionByIdAsync(int sessionId);
    Task<Result<ChatResponseDto>> SendMessageAsync(int sessionId, SendChatMessageDto dto);
    Task<Result<bool>> DeleteSessionAsync(int sessionId);
    Task<Result<bool>> UpdateSessionTitleAsync(int sessionId, UpdateSessionTitleDto dto);
}
