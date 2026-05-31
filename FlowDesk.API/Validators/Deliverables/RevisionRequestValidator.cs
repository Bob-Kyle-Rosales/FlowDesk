using FlowDesk.Core.DTOs.Deliverables;
using FluentValidation;

namespace FlowDesk.API.Validators.Deliverables;

public class RevisionRequestValidator : AbstractValidator<RevisionRequest>
{
    public RevisionRequestValidator()
    {
        RuleFor(x => x.Notes).NotEmpty().MaximumLength(2000);
    }
}
