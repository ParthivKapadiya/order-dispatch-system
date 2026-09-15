using System.ComponentModel.DataAnnotations;
using FluentValidation;

namespace ToplandERP.Web.ViewModels.Account;

public sealed class LoginViewModel
{
    [Display(Name = "Username")]
    public string UserName { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Remember me")]
    public bool RememberMe { get; set; }

    public string? ReturnUrl { get; set; }
}

public sealed class LoginViewModelValidator : AbstractValidator<LoginViewModel>
{
    public LoginViewModelValidator()
    {
        RuleFor(model => model.UserName)
            .NotEmpty()
            .WithMessage("Username is required.")
            .MaximumLength(256);

        RuleFor(model => model.Password)
            .NotEmpty()
            .WithMessage("Password is required.");
    }
}
