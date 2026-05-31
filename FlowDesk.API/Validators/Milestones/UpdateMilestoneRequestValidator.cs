using FlowDesk.Core.DTOs.Milestones;
using FluentValidation;

namespace FlowDesk.API.Validators.Milestones;

public class UpdateMilestoneRequestValidator : AbstractValidator<UpdateMilestoneRequest>
{
    public UpdateMilestoneRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Order).GreaterThanOrEqualTo(0);
    }
}
