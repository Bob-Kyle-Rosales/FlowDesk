using FlowDesk.Core.DTOs.Deliverables;
using FluentValidation;

namespace FlowDesk.API.Validators.Deliverables;

public class CreateDeliverableRequestValidator : AbstractValidator<CreateDeliverableRequest>
{
    public CreateDeliverableRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
    }
}
