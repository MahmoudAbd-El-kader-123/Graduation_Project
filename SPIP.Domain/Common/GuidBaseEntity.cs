namespace SPIP.Domain.Common;

/// <summary>
/// Abstract base entity for domain entities that use a GUID primary key.
/// Used by AIChat entities to satisfy the GUID-identifier requirement
/// without modifying BaseEntity (which uses int PKs for all other entities).
/// </summary>
public abstract class GuidBaseEntity
{
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; } = false;
}
