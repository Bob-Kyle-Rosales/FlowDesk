using FlowDesk.Core.DTOs.Projects;
using FluentValidation;

namespace FlowDesk.API.Validators.Projects;

public class UpdateProjectRequestValidator : AbstractValidator<UpdateProjectRequest>
{
    private static readonly string[] ValidStatuses = ["Active", "Paused", "Completed"];

    public UpdateProjectRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Status)
            .NotEmpty()
            .Must(s => ValidStatuses.Contains(s))
            .WithMessage("Status must be Active, Paused, or Completed.");
    }
}
