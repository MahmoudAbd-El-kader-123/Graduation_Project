using SPIP.Domain.Common;

namespace SPIP.Domain.Entities;

public class AIChatSession : BaseEntity
{
    public int UserId { get; set; }
    public User? User { get; set; }
    public string Title { get; set; } = "New Chat";
    public bool IsArchived { get; set; } = false;

    public ICollection<AIChatMessage> Messages { get; set; } = new List<AIChatMessage>();
}
