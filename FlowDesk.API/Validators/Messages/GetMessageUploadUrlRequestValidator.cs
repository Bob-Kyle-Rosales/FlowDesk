using FlowDesk.Core.DTOs.Messages;
using FluentValidation;

namespace FlowDesk.API.Validators.Messages;

public class GetMessageUploadUrlRequestValidator : AbstractValidator<GetMessageUploadUrlRequest>
{
    public GetMessageUploadUrlRequestValidator()
    {
        RuleFor(x => x.FileName).NotEmpty().MaximumLength(255);
        RuleFor(x => x.ContentType).NotEmpty().MaximumLength(255);
    }
}
