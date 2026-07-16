namespace SPIP.Application.DTOs.Role;

public class RoleDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<int> PermissionIds { get; set; } = new();
}
