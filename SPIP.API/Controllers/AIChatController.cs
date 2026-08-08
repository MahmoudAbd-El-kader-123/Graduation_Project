using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SPIP.Application.DTOs.AIChat;
using SPIP.Application.Interfaces.Services;
using SPIP.Domain.Constants;
using SPIP.Shared.Pagination;
using SPIP.Shared.Responses;

namespace SPIP.API.Controllers;

[ApiController]
[Route("api/ai-chat")]
[Authorize(Policy = Permissions.AIChat.Use)]
public class AIChatController : ControllerBase
{
    private readonly IAIChatService _chatService;

    public AIChatController(IAIChatService chatService)
    {
        _chatService = chatService;
    }

    /// <summary>
    /// Creates a new AI chat session for the current user.
    /// </summary>
    [HttpPost("sessions")]
    [ProducesResponseType(typeof(ApiResponse<ChatSessionListDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ChatSessionListDto>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<ChatSessionListDto>>> CreateSession([FromBody] CreateChatSessionDto dto)
    {
        var result = await _chatService.CreateSessionAsync(dto);
        return result.Succeeded
            ? Ok(ApiResponse<ChatSessionListDto>.SuccessResponse(result.Data!, "Session created successfully."))
            : BadRequest(ApiResponse<ChatSessionListDto>.FailureResponse(result.Error!));
    }

    /// <summary>
    /// Gets all chat sessions for the current user with pagination.
    /// </summary>
    [HttpGet("sessions")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<ChatSessionListDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<ChatSessionListDto>>>> GetSessions([FromQuery] PaginationRequest parameters)
    {
        var result = await _chatService.GetSessionsAsync(parameters);
        return result.Succeeded
            ? Ok(ApiResponse<PagedResult<ChatSessionListDto>>.SuccessResponse(result.Data!))
            : BadRequest(ApiResponse<PagedResult<ChatSessionListDto>>.FailureResponse(result.Error!));
    }

    /// <summary>
    /// Gets a specific chat session with all its messages.
    /// </summary>
    [HttpGet("sessions/{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<ChatSessionDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ChatSessionDetailDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ChatSessionDetailDto>>> GetSessionById(int id)
    {
        var result = await _chatService.GetSessionByIdAsync(id);

        if (!result.Succeeded)
        {
            if (result.Error == "Access denied.")
                return Forbid();

            return NotFound(ApiResponse<ChatSessionDetailDto>.FailureResponse(result.Error!));
        }

        return Ok(ApiResponse<ChatSessionDetailDto>.SuccessResponse(result.Data!));
    }

    /// <summary>
    /// Sends a message to the AI in a specific chat session and returns the AI response.
    /// </summary>
    [HttpPost("sessions/{id:int}/messages")]
    [ProducesResponseType(typeof(ApiResponse<ChatResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ChatResponseDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<ChatResponseDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ChatResponseDto>>> SendMessage(int id, [FromBody] SendChatMessageDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Message))
            return BadRequest(ApiResponse<ChatResponseDto>.FailureResponse("Message cannot be empty."));

        var result = await _chatService.SendMessageAsync(id, dto);

        if (!result.Succeeded)
        {
            if (result.Error == "Access denied.")
                return Forbid();

            if (result.Error == "Session not found.")
                return NotFound(ApiResponse<ChatResponseDto>.FailureResponse(result.Error!));

            return BadRequest(ApiResponse<ChatResponseDto>.FailureResponse(result.Error!));
        }

        return Ok(ApiResponse<ChatResponseDto>.SuccessResponse(result.Data!, "Message sent successfully."));
    }

    /// <summary>
    /// Updates the title of a chat session.
    /// </summary>
    [HttpPut("sessions/{id:int}/title")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<bool>>> UpdateSessionTitle(int id, [FromBody] UpdateSessionTitleDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Title))
            return BadRequest(ApiResponse<bool>.FailureResponse("Title cannot be empty."));

        var result = await _chatService.UpdateSessionTitleAsync(id, dto);

        if (!result.Succeeded)
        {
            if (result.Error == "Access denied.")
                return Forbid();

            return NotFound(ApiResponse<bool>.FailureResponse(result.Error!));
        }

        return Ok(ApiResponse<bool>.SuccessResponse(true, "Session title updated successfully."));
    }

    /// <summary>
    /// Soft-deletes a chat session.
    /// </summary>
    [HttpDelete("sessions/{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteSession(int id)
    {
        var result = await _chatService.DeleteSessionAsync(id);

        if (!result.Succeeded)
        {
            if (result.Error == "Access denied.")
                return Forbid();

            return NotFound(ApiResponse<bool>.FailureResponse(result.Error!));
        }

        return Ok(ApiResponse<bool>.SuccessResponse(true, "Session deleted successfully."));
    }
}
