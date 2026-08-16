namespace SPIP.Domain.Entities;

public class PermissionCatalog
{
    public int Id { get; set; }
    public string SystemName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
}
