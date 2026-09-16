using FluentValidation;

namespace ToplandERP.Application.Dispatching;

public sealed class CompleteDispatchRequestValidator : AbstractValidator<CompleteDispatchRequest>
{
    public CompleteDispatchRequestValidator()
    {
        RuleFor(request => request.DispatchDate)
            .NotEmpty().WithMessage("Dispatch date is required.");

        RuleFor(request => request.DispatchPersonName)
            .NotEmpty().WithMessage("Dispatch person is required.")
            .MaximumLength(200);

        RuleFor(request => request.TransporterId)
            .NotEmpty().WithMessage("Select a transporter.");

        RuleFor(request => request.LrNumber).MaximumLength(80);
        RuleFor(request => request.BookingNumber).MaximumLength(80);
        RuleFor(request => request.Notes).MaximumLength(2000);
    }
}
