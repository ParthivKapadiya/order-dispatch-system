using FluentValidation;
using ToplandERP.Domain.Constants;

namespace ToplandERP.Application.Transporters;

public sealed class TransporterWriteRequestValidator : AbstractValidator<TransporterWriteRequest>
{
    public TransporterWriteRequestValidator()
    {
        RuleFor(request => request.Name)
            .NotEmpty().WithMessage("Transporter name is required.")
            .MaximumLength(200);

        RuleFor(request => request.ContactPerson).MaximumLength(200);
        RuleFor(request => request.Address).MaximumLength(500);
        RuleFor(request => request.Mobile)
            .Matches(ValidationPatterns.Mobile)
            .When(request => !string.IsNullOrWhiteSpace(request.Mobile))
            .WithMessage("Enter a valid 10-digit Indian mobile number.");
    }
}
