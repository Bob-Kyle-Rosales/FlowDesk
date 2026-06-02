using FlowDesk.Core.DTOs.Messages;
using FluentValidation;

namespace FlowDesk.API.Validators.Messages;

public class CreateMessageRequestValidator : AbstractValidator<CreateMessageRequest>
{
    public CreateMessageRequestValidator()
    {
        RuleFor(x => x.Content).NotEmpty().MaximumLength(4000);
    }
}
