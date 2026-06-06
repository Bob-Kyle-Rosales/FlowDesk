using FlowDesk.Core.DTOs.Organisations;
using FluentValidation;

namespace FlowDesk.API.Validators.Organisations;

public class GetLogoUploadUrlRequestValidator : AbstractValidator<GetLogoUploadUrlRequest>
{
    private static readonly string[] AllowedTypes =
        ["image/jpeg", "image/png", "image/webp", "image/svg+xml"];

    public GetLogoUploadUrlRequestValidator()
    {
        RuleFor(x => x.FileName).NotEmpty().MaximumLength(255);
        RuleFor(x => x.ContentType)
            .NotEmpty()
            .MaximumLength(255)
            .Must(ct => AllowedTypes.Contains(ct))
            .WithMessage("ContentType must be an image (jpeg, png, webp, or svg+xml).");
    }
}
