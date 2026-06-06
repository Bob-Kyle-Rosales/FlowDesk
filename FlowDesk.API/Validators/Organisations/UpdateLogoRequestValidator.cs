// FlowDesk.API/Validators/Organisations/UpdateLogoRequestValidator.cs
using FlowDesk.Core.DTOs.Organisations;
using FluentValidation;

namespace FlowDesk.API.Validators.Organisations;

public class UpdateLogoRequestValidator : AbstractValidator<UpdateLogoRequest>
{
    public UpdateLogoRequestValidator()
    {
        RuleFor(x => x.LogoUrl).NotEmpty().MaximumLength(2000);
    }
}
