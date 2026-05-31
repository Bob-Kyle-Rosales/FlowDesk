using FlowDesk.Core.DTOs.Organisations;
using FluentValidation;

namespace FlowDesk.API.Validators.Organisations;

public class UpdateOrganisationRequestValidator : AbstractValidator<UpdateOrganisationRequest>
{
    public UpdateOrganisationRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.PrimaryColor)
            .Matches(@"^#[0-9A-Fa-f]{6}$")
            .When(x => x.PrimaryColor is not null)
            .WithMessage("PrimaryColor must be a valid hex color (e.g. #FF5733).");
    }
}
