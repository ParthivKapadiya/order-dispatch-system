using FluentValidation;
using ToplandERP.Domain.Constants;

namespace ToplandERP.Application.Users;

public sealed class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserRequestValidator()
    {
        RuleFor(request => request.FullName)
            .NotEmpty().WithMessage("Full name is required.")
            .MaximumLength(200);

        RuleFor(request => request.EmployeeCode)
            .NotEmpty().WithMessage("Employee code is required.")
            .MaximumLength(50);

        RuleFor(request => request.UserName)
            .NotEmpty().WithMessage("Username is required.")
            .MinimumLength(3)
            .MaximumLength(256)
            .Matches(@"^[A-Za-z0-9._-]+$")
            .WithMessage("Username may contain letters, numbers, dots, dashes, and underscores.");

        RuleFor(request => request.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress()
            .MaximumLength(256);

        RuleFor(request => request.Mobile)
            .NotEmpty().WithMessage("Mobile number is required.")
            .Matches(ValidationPatterns.Mobile)
            .WithMessage("Enter a valid 10-digit Indian mobile number.");

        RuleFor(request => request.Role)
            .NotEmpty().WithMessage("Role is required.");

        RuleFor(request => request.TemporaryPassword)
            .NotEmpty().WithMessage("Temporary password is required.")
            .Must(PasswordRules.IsStrong)
            .WithMessage(PasswordRules.Message);
    }
}

public sealed class UpdateUserRequestValidator : AbstractValidator<UpdateUserRequest>
{
    public UpdateUserRequestValidator()
    {
        RuleFor(request => request.FullName).NotEmpty().MaximumLength(200);
        RuleFor(request => request.EmployeeCode).NotEmpty().MaximumLength(50);
        RuleFor(request => request.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(request => request.Mobile)
            .NotEmpty()
            .Matches(ValidationPatterns.Mobile)
            .WithMessage("Enter a valid 10-digit Indian mobile number.");
        RuleFor(request => request.Role).NotEmpty();
    }
}

public sealed class ResetUserPasswordRequestValidator : AbstractValidator<ResetUserPasswordRequest>
{
    public ResetUserPasswordRequestValidator()
    {
        RuleFor(request => request.NewPassword)
            .NotEmpty().WithMessage("Password is required.")
            .Must(PasswordRules.IsStrong)
            .WithMessage(PasswordRules.Message);
    }
}

internal static class PasswordRules
{
    public const string Message = "Use at least 8 characters with uppercase, lowercase, a number, and a symbol.";

    public static bool IsStrong(string? password) =>
        !string.IsNullOrEmpty(password)
        && password.Length >= 8
        && password.Any(char.IsUpper)
        && password.Any(char.IsLower)
        && password.Any(char.IsDigit)
        && password.Any(ch => !char.IsLetterOrDigit(ch));
}
