using FluentValidation;

namespace SPIP.Application.DTOs.Role;

public class CreateRoleDto
{
    public string Name { get; set; } = string.Empty;
}

public class CreateRoleValidator : AbstractValidator<CreateRoleDto>
{
    public CreateRoleValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(50);
    }
}
