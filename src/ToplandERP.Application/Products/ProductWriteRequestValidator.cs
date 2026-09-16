using FluentValidation;

namespace ToplandERP.Application.Products;

public sealed class ProductWriteRequestValidator : AbstractValidator<ProductWriteRequest>
{
    public ProductWriteRequestValidator()
    {
        RuleFor(request => request.ProductCode)
            .NotEmpty().WithMessage("Product code is required.")
            .MaximumLength(64)
            .Matches(@"^[A-Za-z0-9][A-Za-z0-9._-]{0,63}$")
            .WithMessage("Product code may contain letters, numbers, dots, dashes, and underscores.");

        RuleFor(request => request.ProductName)
            .NotEmpty().WithMessage("Product name is required.")
            .MaximumLength(200);

        RuleFor(request => request.Category).MaximumLength(100);
        RuleFor(request => request.ModelNumber).MaximumLength(100);
        RuleFor(request => request.Description).MaximumLength(1000);
        RuleFor(request => request.Unit).MaximumLength(30);
    }
}
