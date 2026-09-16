using FluentValidation;

namespace ToplandERP.Application.PaymentConditions;

public sealed class PaymentConditionWriteRequestValidator : AbstractValidator<PaymentConditionWriteRequest>
{
    public PaymentConditionWriteRequestValidator()
    {
        RuleFor(request => request.Name)
            .NotEmpty().WithMessage("Payment condition name is required.")
            .MaximumLength(100);

        RuleFor(request => request.Description).MaximumLength(500);
    }
}
