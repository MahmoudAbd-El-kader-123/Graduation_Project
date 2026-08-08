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

    public async Task<Result<ChatSessionListDto>> CreateSessionAsync(CreateChatSessionDto dto)
    {
        var domainUser = await GetCurrentDomainUserAsync();
        if (domainUser == null)
            return Result<ChatSessionListDto>.Failure("User not found.");

        var session = new AIChatSession
        {
            UserId = domainUser.Id,
            Title = string.IsNullOrWhiteSpace(dto.Title) ? "New Chat" : dto.Title.Trim()
        };

        await _chatRepository.AddAsync(session);
        await _chatRepository.SaveChangesAsync();

        return Result<ChatSessionListDto>.Success(new ChatSessionListDto
        {
            Id = session.Id,
            Title = session.Title,
            CreatedAt = session.CreatedAt,
            UpdatedAt = session.UpdatedAt,
            MessageCount = 0
        });
    }

    public async Task<Result<PagedResult<ChatSessionListDto>>> GetSessionsAsync(PaginationRequest parameters)
    {
        var domainUser = await GetCurrentDomainUserAsync();
        if (domainUser == null)
            return Result<PagedResult<ChatSessionListDto>>.Failure("User not found.");

        var (items, totalCount) = await _chatRepository.GetUserSessionsPagedAsync(domainUser.Id, parameters);

        var dtos = items.Select(s => new ChatSessionListDto
        {
            Id = s.Id,
            Title = s.Title,
            CreatedAt = s.CreatedAt,
            UpdatedAt = s.UpdatedAt,
            LastMessage = s.Messages.FirstOrDefault()?.Content,
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

    public async Task<Result<ChatSessionDetailDto>> GetSessionByIdAsync(int sessionId)
    {
        var domainUser = await GetCurrentDomainUserAsync();
        if (domainUser == null)
            return Result<ChatSessionDetailDto>.Failure("User not found.");

        var session = await _chatRepository.GetWithMessagesByIdAsync(sessionId);
        if (session == null)
            return Result<ChatSessionDetailDto>.Failure("Session not found.");

        if (session.UserId != domainUser.Id)
            return Result<ChatSessionDetailDto>.Failure("Access denied.");

        return Result<ChatSessionDetailDto>.Success(new ChatSessionDetailDto
        {
            Id = session.Id,
            Title = session.Title,
            CreatedAt = session.CreatedAt,
            Messages = session.Messages.Select(MapMessageToDto).ToList()
        });
    }

    public async Task<Result<ChatResponseDto>> SendMessageAsync(int sessionId, SendChatMessageDto dto)
    {
        var domainUser = await GetCurrentDomainUserAsync();
        if (domainUser == null)
            return Result<ChatResponseDto>.Failure("User not found.");

        var session = await _chatRepository.GetWithMessagesByIdAsync(sessionId);
        if (session == null)
            return Result<ChatResponseDto>.Failure("Session not found.");

        if (session.UserId != domainUser.Id)
            return Result<ChatResponseDto>.Failure("Access denied.");

        // 1. Save user message
        var userMessage = new AIChatMessage
        {
            SessionId = sessionId,
            Role = ChatMessageRole.User,
            Content = dto.Message.Trim()
        };
        session.Messages.Add(userMessage);

        // Auto-generate title from first message
        if (session.Title == "New Chat" && session.Messages.Count(m => m.Role == ChatMessageRole.User) == 1)
        {
            session.Title = dto.Message.Trim().Length > 50
                ? dto.Message.Trim()[..50] + "..."
                : dto.Message.Trim();
        }

        session.UpdatedAt = DateTime.UtcNow;
        await _chatRepository.SaveChangesAsync();

        // 2. Call n8n webhook
        string aiResponseText;
        try
        {
            aiResponseText = await CallN8nWebhookAsync(sessionId, domainUser.Id, dto.Message.Trim());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get AI response for session {SessionId}", sessionId);
            aiResponseText = "I'm sorry, I encountered an error processing your request. Please try again.";
        }

        // 3. Save assistant message
        var assistantMessage = new AIChatMessage
        {
            SessionId = sessionId,
            Role = ChatMessageRole.Assistant,
            Content = aiResponseText
        };
        session.Messages.Add(assistantMessage);
        session.UpdatedAt = DateTime.UtcNow;
        await _chatRepository.SaveChangesAsync();

        return Result<ChatResponseDto>.Success(new ChatResponseDto
        {
            SessionId = sessionId,
            UserMessage = MapMessageToDto(userMessage),
            AssistantMessage = MapMessageToDto(assistantMessage)
        });
    }

    public async Task<Result<bool>> DeleteSessionAsync(int sessionId)
    {
        var domainUser = await GetCurrentDomainUserAsync();
        if (domainUser == null)
            return Result<bool>.Failure("User not found.");

        var session = await _chatRepository.GetByIdAsync(sessionId);
        if (session == null)
            return Result<bool>.Failure("Session not found.");

        if (session.UserId != domainUser.Id)
            return Result<bool>.Failure("Access denied.");

        // Soft delete via BaseEntity
        session.IsDeleted = true;
        session.UpdatedAt = DateTime.UtcNow;
        await _chatRepository.UpdateAsync(session);
        await _chatRepository.SaveChangesAsync();

        return Result<bool>.Success(true);
    }

    public async Task<Result<bool>> UpdateSessionTitleAsync(int sessionId, UpdateSessionTitleDto dto)
    {
        var domainUser = await GetCurrentDomainUserAsync();
        if (domainUser == null)
            return Result<bool>.Failure("User not found.");

        var session = await _chatRepository.GetByIdAsync(sessionId);
        if (session == null)
            return Result<bool>.Failure("Session not found.");

        if (session.UserId != domainUser.Id)
            return Result<bool>.Failure("Access denied.");

        session.Title = dto.Title.Trim();
        session.UpdatedAt = DateTime.UtcNow;
        await _chatRepository.UpdateAsync(session);
        await _chatRepository.SaveChangesAsync();

        return Result<bool>.Success(true);
    }

    // ─── Private Helpers ─────────────────────────────────────────────────

    private async Task<User?> GetCurrentDomainUserAsync()
    {
        var identityId = _currentUserService.UserId;
        if (identityId == null) return null;
        return await _userRepository.GetByIdentityIdAsync(identityId.Value);
    }

    private async Task<string> CallN8nWebhookAsync(int sessionId, int userId, string message)
    {
        var webhookUrl = _aiSettings.ChatWebhookEndpoint;
        if (string.IsNullOrWhiteSpace(webhookUrl))
            throw new InvalidOperationException("AIService:ChatWebhookEndpoint is not configured.");

        var payload = new
        {
            sessionId,
            userId,
            message
        };

        _logger.LogInformation("Calling n8n webhook for session {SessionId}, user {UserId}", sessionId, userId);

        var response = await _httpClient.PostAsJsonAsync(webhookUrl, payload);
        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync();

        // n8n can return the response in different formats depending on workflow config.
        // Try to parse as JSON first, fall back to raw text.
        try
        {
            var jsonDoc = JsonDocument.Parse(responseBody);
            var root = jsonDoc.RootElement;

            // n8n chat trigger typically returns { "output": "..." }
            if (root.TryGetProperty("output", out var outputProp))
                return outputProp.GetString() ?? responseBody;

            // Or it might return { "text": "..." }
            if (root.TryGetProperty("text", out var textProp))
                return textProp.GetString() ?? responseBody;

            // Or { "response": "..." }
            if (root.TryGetProperty("response", out var responseProp))
                return responseProp.GetString() ?? responseBody;

            // If it's a simple string JSON value
            if (root.ValueKind == JsonValueKind.String)
                return root.GetString() ?? responseBody;

            return responseBody;
        }
        catch (JsonException)
        {
            // Not JSON, return raw text
            return responseBody;
        }
    }

    private static ChatMessageDto MapMessageToDto(AIChatMessage m) => new()
    {
        Id = m.Id,
        Role = m.Role.ToString(),
        Content = m.Content,
        CreatedAt = m.CreatedAt,
        TokensUsed = m.TokensUsed
    };
}
