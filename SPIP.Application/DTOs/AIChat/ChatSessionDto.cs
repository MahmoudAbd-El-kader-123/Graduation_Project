using System.ComponentModel.DataAnnotations;

namespace SPIP.Application.DTOs.AIChat;

/// <summary>
/// List item DTO for chat sessions (no messages included).
/// </summary>
public class ChatSessionListDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    /// <summary>Content preview of the last message, truncated to 100 characters.</summary>
    public string? LastMessage { get; set; }

    public int MessageCount { get; set; }
}

/// <summary>
/// Full session detail with all messages.
/// </summary>
public class ChatSessionDetailDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public List<ChatMessageDto> Messages { get; set; } = [];
}

/// <summary>
/// Individual chat message DTO.
/// </summary>
public class ChatMessageDto
{
    public Guid Id { get; set; }

    /// <summary>"User" | "Assistant" | "System"</summary>
    public string Role { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Request DTO for creating a new chat session.
/// </summary>
public class CreateChatSessionDto
{
    /// <summary>Optional title; defaults to "New Chat" if not provided.</summary>
    [MaxLength(200, ErrorMessage = "Session title cannot exceed 200 characters.")]
    public string? Title { get; set; }
}

/// <summary>
/// Request DTO for sending a message in a chat session.
/// Validated by SendChatMessageDtoValidator.
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
    public Guid SessionId { get; set; }
    public ChatMessageDto UserMessage { get; set; } = null!;
    public ChatMessageDto AssistantMessage { get; set; } = null!;
}

/// <summary>
/// Request DTO for updating a session title.
/// Validated by UpdateSessionTitleDtoValidator.
/// </summary>
public class UpdateSessionTitleDto
{
    public string Title { get; set; } = string.Empty;
}
