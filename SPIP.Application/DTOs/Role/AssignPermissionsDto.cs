using FluentValidation;

namespace SPIP.Application.DTOs.Role;

public class AssignPermissionsDto
{
    public List<int> PermissionIds { get; set; } = new();
}

public class AssignPermissionsValidator : AbstractValidator<AssignPermissionsDto>
{
    public AssignPermissionsValidator()
    {
        RuleFor(x => x.PermissionIds).NotNull();
    }
}
