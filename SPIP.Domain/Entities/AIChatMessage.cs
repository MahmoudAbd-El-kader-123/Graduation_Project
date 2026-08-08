using SPIP.Domain.Common;
using SPIP.Domain.Enums;

namespace SPIP.Domain.Entities;

public class AIChatMessage : GuidBaseEntity
{
    /// <summary>
    /// FK to AIChatSession.Id — Guid, matching the session's Guid PK.
    /// </summary>
    public Guid SessionId { get; set; }
    public AIChatSession? Session { get; set; }

    public ChatMessageRole Role { get; set; }

    /// Message content. Unbounded length (nvarchar(max)) configured at DB level.
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Optional token count returned by the AI provider for future token accounting.
    /// </summary>
    public int? TokensUsed { get; set; }
}
