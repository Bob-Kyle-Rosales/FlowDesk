using FlowDesk.Core.DTOs.Milestones;
using FluentValidation;

namespace FlowDesk.API.Validators.Milestones;

public class CreateMilestoneRequestValidator : AbstractValidator<CreateMilestoneRequest>
{
    public CreateMilestoneRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Order).GreaterThanOrEqualTo(0);
    }
}
