namespace SPIP.Application.DTOs.Permission;

public class PermissionDto
{
    public int Id { get; set; }
    public string SystemName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
}

public class PermissionGroupDto
{
    public string ModuleName { get; set; } = string.Empty;
    public List<PermissionDto> Permissions { get; set; } = new();
}
