using FlowDesk.Core.DTOs.Messages;
using FluentValidation;

namespace FlowDesk.API.Validators.Messages;

public class CreateMessageRequestValidator : AbstractValidator<CreateMessageRequest>
{
    public CreateMessageRequestValidator()
    {
        RuleFor(x => x.Content).NotEmpty().MaximumLength(4000);
        RuleFor(x => x.FileUrl).MaximumLength(2000).When(x => x.FileUrl is not null);
    }
}
