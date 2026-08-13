using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SPIP.Application.DTOs.AIChat;
using SPIP.Application.Interfaces.Repositories;
using SPIP.Application.Interfaces.Services;
using SPIP.Domain.Entities;
using SPIP.Domain.Enums;
using SPIP.Infrastructure.Configuration;
using SPIP.Shared.Pagination;
using SPIP.Shared.Result;

namespace SPIP.Infrastructure.Services;

public class AIChatService : IAIChatService
{
    private readonly IAIChatRepository _chatRepository;
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly HttpClient _httpClient;
    private readonly AIServiceSettings _aiSettings;
    private readonly ILogger<AIChatService> _logger;

    // Maximum acceptable AI response length (characters). Responses beyond this are rejected.
    private const int MaxAiResponseLength = 100_000;

    public AIChatService(
        IAIChatRepository chatRepository,
        IUserRepository userRepository,
        ICurrentUserService currentUserService,
        HttpClient httpClient,
        IOptions<AIServiceSettings> aiSettings,
        ILogger<AIChatService> logger)
    {
        _chatRepository = chatRepository;
        _userRepository = userRepository;
        _currentUserService = currentUserService;
        _httpClient = httpClient;
        _aiSettings = aiSettings.Value;
        _logger = logger;
    }

    // ─── Public Service Methods ───────────────────────────────────────────────

    public async Task<Result<ChatSessionListDto>> CreateSessionAsync(
        CreateChatSessionDto dto, CancellationToken ct = default)
    {
        var domainUser = await GetCurrentDomainUserAsync(ct);
        if (domainUser == null)
            return Result<ChatSessionListDto>.Failure("User not found.");

        var session = new AIChatSession
        {
            UserId = domainUser.Id,
            Title = string.IsNullOrWhiteSpace(dto.Title) ? "New Chat" : dto.Title.Trim()
        };

        await _chatRepository.AddAsync(session, ct);
        await _chatRepository.SaveChangesAsync(ct);

        return Result<ChatSessionListDto>.Success(new ChatSessionListDto
        {
            Id = session.Id,
            Title = session.Title,
            CreatedAt = session.CreatedAt,
            UpdatedAt = session.UpdatedAt,
            MessageCount = 0
        });
    }

    public async Task<Result<PagedResult<ChatSessionListDto>>> GetSessionsAsync(
        PaginationRequest parameters, CancellationToken ct = default)
    {
        var domainUser = await GetCurrentDomainUserAsync(ct);
        if (domainUser == null)
            return Result<PagedResult<ChatSessionListDto>>.Failure("User not found.");

        var (items, totalCount) = await _chatRepository.GetUserSessionsPagedAsync(domainUser.Id, parameters, ct);

        var dtos = items.Select(s => new ChatSessionListDto
        {
            Id = s.Id,
            Title = s.Title,
            CreatedAt = s.CreatedAt,
            UpdatedAt = s.UpdatedAt,
            // Truncate to 100 chars to keep the list response lightweight
            LastMessage = s.Messages.FirstOrDefault()?.Content is { } content
                ? (content.Length > 100 ? content[..100] + "…" : content)
                : null,
            MessageCount = s.Messages.Count
        }).ToList();

        return Result<PagedResult<ChatSessionListDto>>.Success(new PagedResult<ChatSessionListDto>
        {
            Items = dtos,
            TotalCount = totalCount,
            PageNumber = parameters.PageNumber,
            PageSize = parameters.PageSize
        });
    }

    public async Task<Result<ChatSessionDetailDto>> GetSessionByIdAsync(
        Guid sessionId, CancellationToken ct = default)
    {
        var domainUser = await GetCurrentDomainUserAsync(ct);
        if (domainUser == null)
            return Result<ChatSessionDetailDto>.Failure("User not found.");

        // Auth-aware query: returns null for both "not found" and "not owned" → unified 404
        var session = await _chatRepository.GetSessionWithMessagesForUserAsync(sessionId, domainUser.Id, ct);
        if (session == null)
            return Result<ChatSessionDetailDto>.Failure("Session not found.");

        return Result<ChatSessionDetailDto>.Success(new ChatSessionDetailDto
        {
            Id = session.Id,
            Title = session.Title,
            CreatedAt = session.CreatedAt,
            Messages = session.Messages.Select(MapMessageToDto).ToList()
        });
    }

    public async Task<Result<ChatResponseDto>> SendMessageAsync(
        Guid sessionId, SendChatMessageDto dto, CancellationToken ct = default)
    {
        var domainUser = await GetCurrentDomainUserAsync(ct);
        if (domainUser == null)
            return Result<ChatResponseDto>.Failure("User not found.");

        // Auth-aware query: includes messages so n8n gets full conversation history context.
        // NOTE: For sessions with very long histories this may need pagination in a future sprint.
        var session = await _chatRepository.GetSessionWithMessagesForUserAsync(sessionId, domainUser.Id, ct);
        if (session == null)
            return Result<ChatResponseDto>.Failure("Session not found.");

        var trimmedMessage = dto.Message.Trim();

        // ── 1. Persist user message ───────────────────────────────────────────
        var userMessage = new AIChatMessage
        {
            SessionId = sessionId,
            Role = ChatMessageRole.User,
            Content = trimmedMessage
        };
        session.Messages.Add(userMessage);

        // Auto-generate title from the first user message when the session still has the default title
        if (session.Title == "New Chat" && session.Messages.Count(m => m.Role == ChatMessageRole.User) == 1)
        {
            session.Title = trimmedMessage.Length > 50
                ? trimmedMessage[..50] + "…"
                : trimmedMessage;
        }

        session.UpdatedAt = DateTime.UtcNow;
        await _chatRepository.SaveChangesAsync(ct);   // SaveChanges #1 — user message committed

        // ── 2. Call n8n webhook ───────────────────────────────────────────────
        string aiResponseText;
        try
        {
            aiResponseText = await CallN8nWebhookAsync(
                sessionId,
                domainUser.Id,           // domain integer user ID — preserved n8n contract field
                trimmedMessage,
                ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // Client disconnected — stop processing, do not persist a fallback message
            _logger.LogInformation("AI chat request cancelled by client for session {SessionId}", sessionId);
            throw;
        }
        catch (TimeoutException ex)
        {
            _logger.LogWarning(ex, "n8n webhook timed out for session {SessionId}", sessionId);
            return Result<ChatResponseDto>.Failure("AI service timed out. Please try again.");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "n8n webhook failed for session {SessionId} — status {StatusCode}",
                sessionId, ex.StatusCode);
            return Result<ChatResponseDto>.Failure("AI service is unavailable. Please try again later.");
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "AI response validation failed for session {SessionId}", sessionId);
            return Result<ChatResponseDto>.Failure("AI returned an unexpected response. Please try again.");
        }

        // ── 3. Persist assistant message ──────────────────────────────────────
        var assistantMessage = new AIChatMessage
        {
            SessionId = sessionId,
            Role = ChatMessageRole.Assistant,
            Content = aiResponseText
        };
        session.Messages.Add(assistantMessage);
        session.UpdatedAt = DateTime.UtcNow;
        await _chatRepository.SaveChangesAsync(ct);   // SaveChanges #2 — assistant message committed

        return Result<ChatResponseDto>.Success(new ChatResponseDto
        {
            SessionId = sessionId,
            UserMessage = MapMessageToDto(userMessage),
            AssistantMessage = MapMessageToDto(assistantMessage)
        });
    }

    public async Task<Result<bool>> DeleteSessionAsync(Guid sessionId, CancellationToken ct = default)
    {
        var domainUser = await GetCurrentDomainUserAsync(ct);
        if (domainUser == null)
            return Result<bool>.Failure("User not found.");

        // Auth-aware query — no need for a separate ownership check
        var session = await _chatRepository.GetSessionForUserAsync(sessionId, domainUser.Id, ct);
        if (session == null)
            return Result<bool>.Failure("Session not found.");

        // Soft-delete: the global query filter will exclude this session from all future queries
        session.IsDeleted = true;
        session.UpdatedAt = DateTime.UtcNow;

        await _chatRepository.UpdateAsync(session);
        await _chatRepository.SaveChangesAsync(ct);

        return Result<bool>.Success(true);
    }

    public async Task<Result<bool>> UpdateSessionTitleAsync(
        Guid sessionId, UpdateSessionTitleDto dto, CancellationToken ct = default)
    {
        var domainUser = await GetCurrentDomainUserAsync(ct);
        if (domainUser == null)
            return Result<bool>.Failure("User not found.");

        var session = await _chatRepository.GetSessionForUserAsync(sessionId, domainUser.Id, ct);
        if (session == null)
            return Result<bool>.Failure("Session not found.");

        session.Title = dto.Title.Trim();
        session.UpdatedAt = DateTime.UtcNow;

        await _chatRepository.UpdateAsync(session);
        await _chatRepository.SaveChangesAsync(ct);

        return Result<bool>.Success(true);
    }

    // ─── Private Helpers ──────────────────────────────────────────────────────

    private async Task<User?> GetCurrentDomainUserAsync(CancellationToken ct)
    {
        var identityId = _currentUserService.UserId;
        if (identityId == null) return null;
        return await _userRepository.GetByIdentityIdAsync(identityId.Value);
    }

    /// <summary>
    /// Calls the n8n AI chat webhook.
    ///
    /// PRESERVED n8n CONTRACT:
    ///   Field names: sessionId, userId, message — unchanged.
    ///   userId: domain integer user ID (User.Id) — same as the original implementation.
    ///   sessionId: Guid string — TYPE CHANGED from int because AIChatSession.Id is now Guid.
    ///              This is the only unavoidable breaking change in the n8n payload.
    ///              The n8n workflow must be updated to accept a Guid string for sessionId.
    ///   message: exact user input — unchanged.
    ///
    /// The backend derives userId from the authenticated JWT — never from client input.
    /// </summary>
    private async Task<string> CallN8nWebhookAsync(
        Guid sessionId,
        int domainUserId,      // domain integer user ID — same value as original contract
        string userMessage,
        CancellationToken ct)
    {
        var webhookEndpoint = _aiSettings.ChatWebhookEndpoint;   // preserved config key name
        if (string.IsNullOrWhiteSpace(webhookEndpoint))
            throw new InvalidOperationException("AIService:ChatWebhookEndpoint is not configured.");

        // ── n8n request payload ───────────────────────────────────────────────
        // Preserved contract:
        //   sessionId: Guid string  (⚠ type changed from int — n8n workflow must accept Guid string)
        //   userId:    int          (unchanged — domain user integer ID, derived from JWT)
        //   message:   string       (unchanged — exact user input)
        var payload = new
        {
            sessionId = sessionId.ToString(),  // Guid string — see note above
            userId = domainUserId,             // int, same as original contract
            message = userMessage              // do NOT log this value
        };

        // Inject X-API-Key header for secret webhook authentication
        _httpClient.DefaultRequestHeaders.Remove("X-API-Key");
        if (!string.IsNullOrWhiteSpace(_aiSettings.ApiKey))
        {
            _httpClient.DefaultRequestHeaders.Add("X-API-Key", _aiSettings.ApiKey);
        }

        // Optional: authenticate request to n8n with a bearer secret.
        if (!string.IsNullOrWhiteSpace(_aiSettings.WebhookSecret))
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _aiSettings.WebhookSecret);
        }

        _logger.LogInformation(
            "Calling n8n webhook for session {SessionId}, userId {UserId}",
            sessionId, domainUserId);

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.PostAsJsonAsync(webhookEndpoint, payload, ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;   // propagate client-disconnect cancellation
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            // HttpClient timeout (not a client-requested cancel)
            throw new TimeoutException("n8n webhook request timed out.", ex);
        }

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "n8n returned non-success HTTP {StatusCode} for session {SessionId}",
                (int)response.StatusCode, sessionId);
            throw new HttpRequestException(
                $"AI service returned HTTP {(int)response.StatusCode}",
                inner: null,
                statusCode: response.StatusCode);
        }

        var responseBody = await response.Content.ReadAsStringAsync(ct);

        // Guard against excessively large responses
        if (responseBody.Length > MaxAiResponseLength)
        {
            _logger.LogWarning(
                "n8n response exceeded maximum length ({Length} chars) for session {SessionId}",
                responseBody.Length, sessionId);
            throw new InvalidOperationException("AI response was too large.");
        }

        var parsed = ParseN8nResponse(responseBody, sessionId);

        _logger.LogInformation(
            "n8n AI chat webhook completed successfully for session {SessionId}", sessionId);

        return parsed;
    }

    /// <summary>
    /// Parses the n8n webhook response body.
    /// n8n can return the AI output in several JSON shapes depending on workflow configuration.
    /// </summary>
    private string ParseN8nResponse(string responseBody, Guid sessionId)
    {
        if (string.IsNullOrWhiteSpace(responseBody))
            throw new InvalidOperationException("AI service returned an empty response.");

        try
        {
            var jsonDoc = JsonDocument.Parse(responseBody);
            var root = jsonDoc.RootElement;

            // n8n chat trigger typically returns { "output": "..." }
            if (root.TryGetProperty("output", out var outputProp) && outputProp.ValueKind == JsonValueKind.String)
                return outputProp.GetString()!;

            // Or { "text": "..." }
            if (root.TryGetProperty("text", out var textProp) && textProp.ValueKind == JsonValueKind.String)
                return textProp.GetString()!;

            // Or { "response": "..." }
            if (root.TryGetProperty("response", out var responseProp) && responseProp.ValueKind == JsonValueKind.String)
                return responseProp.GetString()!;

            // Or a simple JSON string value
            if (root.ValueKind == JsonValueKind.String)
                return root.GetString()!;

            // Unrecognised JSON structure — reject
            _logger.LogWarning("Unrecognised n8n response structure for session {SessionId}", sessionId);
            throw new InvalidOperationException("AI service returned an unrecognised response format.");
        }
        catch (JsonException)
        {
            // n8n returned plain text, not JSON — treat as the response directly
            return responseBody;
        }
    }

    private static ChatMessageDto MapMessageToDto(AIChatMessage m) => new()
    {
        Id = m.Id,
        Role = m.Role.ToString(),
        Content = m.Content,
        CreatedAt = m.CreatedAt
    };
}
