using FlowDesk.Core.DTOs.Milestones;
using FluentValidation;

namespace FlowDesk.API.Validators.Milestones;

public class UpdateMilestoneStatusRequestValidator : AbstractValidator<UpdateMilestoneStatusRequest>
{
    private static readonly string[] ValidStatuses = new[] { "Pending", "InProgress", "Completed" };

    public UpdateMilestoneStatusRequestValidator()
    {
        RuleFor(x => x.Status)
            .NotEmpty()
            .Must(s => ValidStatuses.Contains(s))
            .WithMessage("Status must be Pending, InProgress, or Completed.");
    }
}
