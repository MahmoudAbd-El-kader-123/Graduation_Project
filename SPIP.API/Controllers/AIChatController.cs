using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
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
    /// Returns 201 Created with a Location header pointing to the new session resource.
    /// </summary>
    [HttpPost("sessions")]
    [ProducesResponseType(typeof(ApiResponse<ChatSessionListDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<ChatSessionListDto>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<ChatSessionListDto>>> CreateSession(
        [FromBody] CreateChatSessionDto dto,
        CancellationToken ct)
    {
        var result = await _chatService.CreateSessionAsync(dto, ct);
        if (!result.Succeeded)
            return BadRequest(ApiResponse<ChatSessionListDto>.FailureResponse(result.Error!));

        return CreatedAtAction(
            nameof(GetSessionById),
            new { id = result.Data!.Id },
            ApiResponse<ChatSessionListDto>.SuccessResponse(result.Data, "Session created successfully."));
    }

    /// <summary>
    /// Gets all chat sessions for the current user with pagination.
    /// Sessions are ordered by most recently updated first.
    /// </summary>
    [HttpGet("sessions")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<ChatSessionListDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<ChatSessionListDto>>>> GetSessions(
        [FromQuery] PaginationRequest parameters,
        CancellationToken ct)
    {
        var result = await _chatService.GetSessionsAsync(parameters, ct);
        return result.Succeeded
            ? Ok(ApiResponse<PagedResult<ChatSessionListDto>>.SuccessResponse(result.Data!))
            : BadRequest(ApiResponse<PagedResult<ChatSessionListDto>>.FailureResponse(result.Error!));
    }

    /// <summary>
    /// Gets a specific chat session with all its messages.
    /// Returns 404 if the session does not exist or does not belong to the current user.
    /// </summary>
    [HttpGet("sessions/{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<ChatSessionDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ChatSessionDetailDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ChatSessionDetailDto>>> GetSessionById(
        Guid id,
        CancellationToken ct)
    {
        var result = await _chatService.GetSessionByIdAsync(id, ct);
        return result.Succeeded
            ? Ok(ApiResponse<ChatSessionDetailDto>.SuccessResponse(result.Data!))
            : NotFound(ApiResponse<ChatSessionDetailDto>.FailureResponse(result.Error!));
    }

    /// <summary>
    /// Sends a message to the AI in a specific chat session and returns the AI response.
    /// This endpoint is rate-limited (AIChatPolicy) to prevent abuse of expensive AI inference.
    /// </summary>
    [HttpPost("sessions/{id:guid}/messages")]
    [EnableRateLimiting("AIChatPolicy")]
    [ProducesResponseType(typeof(ApiResponse<ChatResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ChatResponseDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<ChatResponseDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<ChatResponseDto>), StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ApiResponse<ChatResponseDto>), StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<ApiResponse<ChatResponseDto>>> SendMessage(
        Guid id,
        [FromBody] SendChatMessageDto dto,
        CancellationToken ct)
    {
        var result = await _chatService.SendMessageAsync(id, dto, ct);

        if (!result.Succeeded)
        {
            // Map specific error messages to appropriate HTTP semantics
            return result.Error switch
            {
                "Session not found."
                    => NotFound(ApiResponse<ChatResponseDto>.FailureResponse(result.Error)),
                "AI service timed out. Please try again." or
                "AI service is unavailable. Please try again later."
                    => StatusCode(StatusCodes.Status503ServiceUnavailable,
                        ApiResponse<ChatResponseDto>.FailureResponse(result.Error)),
                "AI returned an unexpected response. Please try again."
                    => StatusCode(StatusCodes.Status502BadGateway,
                        ApiResponse<ChatResponseDto>.FailureResponse(result.Error)),
                _   => BadRequest(ApiResponse<ChatResponseDto>.FailureResponse(result.Error!))
            };
        }

        return Ok(ApiResponse<ChatResponseDto>.SuccessResponse(result.Data!, "Message sent successfully."));
    }

    /// <summary>
    /// Updates the title of a chat session.
    /// Returns 404 if the session does not exist or does not belong to the current user.
    /// </summary>
    [HttpPut("sessions/{id:guid}/title")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<bool>>> UpdateSessionTitle(
        Guid id,
        [FromBody] UpdateSessionTitleDto dto,
        CancellationToken ct)
    {
        var result = await _chatService.UpdateSessionTitleAsync(id, dto, ct);

        if (!result.Succeeded)
        {
            return result.Error == "Session not found."
                ? NotFound(ApiResponse<bool>.FailureResponse(result.Error))
                : BadRequest(ApiResponse<bool>.FailureResponse(result.Error!));
        }

        return Ok(ApiResponse<bool>.SuccessResponse(true, "Session title updated successfully."));
    }

    /// <summary>
    /// Soft-deletes a chat session. Associated messages are cascade-deleted at the database level.
    /// Returns 404 if the session does not exist or does not belong to the current user.
    /// </summary>
    [HttpDelete("sessions/{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteSession(Guid id, CancellationToken ct)
    {
        var result = await _chatService.DeleteSessionAsync(id, ct);

        return result.Succeeded
            ? Ok(ApiResponse<bool>.SuccessResponse(true, "Session deleted successfully."))
            : NotFound(ApiResponse<bool>.FailureResponse(result.Error!));
    }
}
