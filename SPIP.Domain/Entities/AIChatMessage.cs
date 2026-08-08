using SPIP.Domain.Common;
using SPIP.Domain.Enums;

namespace SPIP.Domain.Entities;

public class AIChatMessage : BaseEntity
{
    public int SessionId { get; set; }
    public AIChatSession? Session { get; set; }
    public ChatMessageRole Role { get; set; }
    public string Content { get; set; } = string.Empty;
    public int? TokensUsed { get; set; }
}
