using FluentValidation;
using SPIP.Application.DTOs.AIChat;

namespace SPIP.Application.Validators;

public class SendChatMessageDtoValidator : AbstractValidator<SendChatMessageDto>
{
    public SendChatMessageDtoValidator()
    {
        RuleFor(x => x.Message)
            .NotEmpty().WithMessage("Message cannot be empty.")
            .MaximumLength(4000).WithMessage("Message cannot exceed 4000 characters.");
    }
}

public class UpdateSessionTitleDtoValidator : AbstractValidator<UpdateSessionTitleDto>
{
    public UpdateSessionTitleDtoValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title cannot be empty.")
            .MaximumLength(200).WithMessage("Title cannot exceed 200 characters.");
    }
}

public class CreateChatSessionDtoValidator : AbstractValidator<CreateChatSessionDto>
{
    public CreateChatSessionDtoValidator()
    {
        RuleFor(x => x.Title)
            .MaximumLength(200).WithMessage("Title cannot exceed 200 characters.")
            .When(x => x.Title != null);
    }
}
