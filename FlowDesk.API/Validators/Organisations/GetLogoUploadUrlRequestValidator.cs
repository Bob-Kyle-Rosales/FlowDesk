using FlowDesk.Core.DTOs.Organisations;
using FluentValidation;

namespace FlowDesk.API.Validators.Organisations;

public class GetLogoUploadUrlRequestValidator : AbstractValidator<GetLogoUploadUrlRequest>
{
    public GetLogoUploadUrlRequestValidator()
    {
        RuleFor(x => x.FileName).NotEmpty().MaximumLength(255);
        RuleFor(x => x.ContentType).NotEmpty().MaximumLength(255);
    }
}
