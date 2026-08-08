namespace SPIP.Application.DTOs.AIChat;

/// <summary>
/// List item DTO for chat sessions (no messages included).
/// </summary>
public class ChatSessionListDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? LastMessage { get; set; }
    public int MessageCount { get; set; }
}

/// <summary>
/// Full session detail with all messages.
/// </summary>
public class ChatSessionDetailDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public List<ChatMessageDto> Messages { get; set; } = [];
}

/// <summary>
/// Individual chat message DTO.
/// </summary>
public class ChatMessageDto
{
    public int Id { get; set; }
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int? TokensUsed { get; set; }
}

/// <summary>
/// Request DTO for creating a new chat session.
/// </summary>
public class CreateChatSessionDto
{
    public string? Title { get; set; }
}

/// <summary>
/// Request DTO for sending a message in a chat session.
/// </summary>
public class SendChatMessageDto
{
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Response DTO after sending a message — contains both the user and assistant messages.
/// </summary>
public class ChatResponseDto
{
    public int SessionId { get; set; }
    public ChatMessageDto UserMessage { get; set; } = null!;
    public ChatMessageDto AssistantMessage { get; set; } = null!;
}

/// <summary>
/// Request DTO for updating a session title.
/// </summary>
public class UpdateSessionTitleDto
{
    public string Title { get; set; } = string.Empty;
}
