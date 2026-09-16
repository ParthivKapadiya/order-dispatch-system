using FluentValidation;
using ToplandERP.Domain.Constants;

namespace ToplandERP.Application.Customers;

public sealed class CustomerWriteRequestValidator : AbstractValidator<CustomerWriteRequest>
{
    public CustomerWriteRequestValidator()
    {
        RuleFor(request => request.CustomerCode)
            .NotEmpty().WithMessage("Customer code is required.")
            .MaximumLength(50);

        RuleFor(request => request.CustomerName)
            .NotEmpty().WithMessage("Customer name is required.")
            .MaximumLength(200);

        RuleFor(request => request.Mobile)
            .NotEmpty().WithMessage("Mobile number is required.")
            .Matches(ValidationPatterns.Mobile).WithMessage("Enter a valid 10-digit Indian mobile number.");

        RuleFor(request => request.Email)
            .EmailAddress().When(request => !string.IsNullOrWhiteSpace(request.Email))
            .MaximumLength(256);

        RuleFor(request => request.BillingAddress)
            .NotEmpty().WithMessage("Billing address is required.")
            .MaximumLength(500);
        RuleFor(request => request.BillingCity).NotEmpty().MaximumLength(100);
        RuleFor(request => request.BillingState).NotEmpty().MaximumLength(100);
        RuleFor(request => request.BillingPincode)
            .NotEmpty()
            .Matches(ValidationPatterns.Pincode).WithMessage("Enter a valid 6-digit pincode.");

        RuleFor(request => request.DeliveryAddress)
            .NotEmpty().WithMessage("Delivery address is required.")
            .MaximumLength(500);
        RuleFor(request => request.DeliveryCity).NotEmpty().MaximumLength(100);
        RuleFor(request => request.DeliveryState).NotEmpty().MaximumLength(100);
        RuleFor(request => request.DeliveryPincode)
            .NotEmpty()
            .Matches(ValidationPatterns.Pincode).WithMessage("Enter a valid 6-digit pincode.");
    }
}
