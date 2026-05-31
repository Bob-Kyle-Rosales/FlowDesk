using FlowDesk.Core.DTOs.Projects;
using FluentValidation;

namespace FlowDesk.API.Validators.Projects;

public class CreateProjectRequestValidator : AbstractValidator<CreateProjectRequest>
{
    public CreateProjectRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ClientId).NotEqual(Guid.Empty).WithMessage("ClientId is required.");
    }
}
