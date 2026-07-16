namespace SPIP.Application.DTOs.User;

public class UpdateUserDto
{
    public string FullName { get; set; } = string.Empty;
    public Guid RoleId { get; set; }
    public bool IsActive { get; set; }
}
