using SPIP.Domain.Common;

namespace SPIP.Domain.Entities;

public class AIChatSession : GuidBaseEntity
{
    /// <summary>
    /// Domain user ID (int FK to Users_Domain). Kept as int to match User.Id convention.
    /// </summary>
    public int UserId { get; set; }
    public User? User { get; set; }

    public string Title { get; set; } = "New Chat";
    public bool IsArchived { get; set; } = false;

    public ICollection<AIChatMessage> Messages { get; set; } = new List<AIChatMessage>();
}
