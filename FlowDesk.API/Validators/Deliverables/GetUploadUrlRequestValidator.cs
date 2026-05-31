using FlowDesk.Core.DTOs.Deliverables;
using FluentValidation;

namespace FlowDesk.API.Validators.Deliverables;

public class GetUploadUrlRequestValidator : AbstractValidator<GetUploadUrlRequest>
{
    public GetUploadUrlRequestValidator()
    {
        RuleFor(x => x.FileName).NotEmpty().MaximumLength(255);
        RuleFor(x => x.ContentType).NotEmpty().MaximumLength(255);
    }
}
