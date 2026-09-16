using FluentValidation;
using ToplandERP.Domain.Constants;

namespace ToplandERP.Application.Modifications;

public sealed class CreateModificationRequestValidator : AbstractValidator<CreateModificationRequest>
{
    public CreateModificationRequestValidator()
    {
        RuleFor(request => request.Reason)
            .NotEmpty().WithMessage("Reason for modification is required.")
            .MaximumLength(2000);

        RuleFor(request => request.BillingAddress)
            .NotEmpty().WithMessage("Billing address is required.")
            .MaximumLength(500);
        RuleFor(request => request.BillingCity).NotEmpty().MaximumLength(100);
        RuleFor(request => request.BillingState).NotEmpty().MaximumLength(100);
        RuleFor(request => request.BillingPincode)
            .NotEmpty()
            .Matches(ValidationPatterns.Pincode).WithMessage("Enter a valid 6-digit billing pincode.");

        RuleFor(request => request.DeliveryAddress)
            .NotEmpty().WithMessage("Delivery address is required.")
            .MaximumLength(500);
        RuleFor(request => request.DeliveryCity).NotEmpty().MaximumLength(100);
        RuleFor(request => request.DeliveryState).NotEmpty().MaximumLength(100);
        RuleFor(request => request.DeliveryPincode)
            .NotEmpty()
            .Matches(ValidationPatterns.Pincode).WithMessage("Enter a valid 6-digit delivery pincode.");

        RuleFor(request => request.Items)
            .Must(items => items.Any(item => item.ProductId.HasValue && item.ProductId.Value != Guid.Empty && item.Quantity > 0))
            .WithMessage("Add at least one product with quantity greater than zero.");

        RuleForEach(request => request.Items)
            .Where(line => line.ProductId.HasValue && line.ProductId.Value != Guid.Empty)
            .ChildRules(line =>
            {
                line.RuleFor(item => item.Quantity)
                    .GreaterThan(0).WithMessage("Quantity must be greater than zero.");
            });

        RuleFor(request => request.BillAmount)
            .GreaterThanOrEqualTo(0).WithMessage("Bill amount cannot be negative.");

        RuleFor(request => request.PaymentConditionId)
            .NotEmpty().WithMessage("Select a payment condition.");

        RuleFor(request => request.TransporterId)
            .NotEmpty().WithMessage("Select a transporter.");

        RuleFor(request => request.BookingNumber).MaximumLength(80);
        RuleFor(request => request.BookingFrom).MaximumLength(150);
        RuleFor(request => request.BookingTo).MaximumLength(150);
        RuleFor(request => request.BookingDetails).MaximumLength(1000);
        RuleFor(request => request.Remarks).MaximumLength(2000);
        RuleFor(request => request.SpecialInstructions).MaximumLength(2000);
    }
}

public sealed class RejectModificationRequestValidator : AbstractValidator<RejectModificationRequest>
{
    public RejectModificationRequestValidator()
    {
        RuleFor(request => request.RejectionReason)
            .NotEmpty().WithMessage("Rejection reason is required.")
            .MaximumLength(2000);
    }
}
