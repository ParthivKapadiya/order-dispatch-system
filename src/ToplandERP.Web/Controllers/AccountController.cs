using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ToplandERP.Infrastructure.Identity;
using ToplandERP.Web.ViewModels.Account;

namespace ToplandERP.Web.Controllers;

public class AccountController : Controller
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        ILogger<AccountController> logger)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _logger = logger;
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToLocal(returnUrl);
        }

        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, CancellationToken cancellationToken)
    {
        model.ReturnUrl ??= Url.Content("~/");

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.FindByNameAsync(model.UserName);
        if (user is null || !user.IsActive)
        {
            ModelState.AddModelError(string.Empty, "Invalid username or password.");
            return View(model);
        }

        var result = await _signInManager.PasswordSignInAsync(
            user,
            model.Password,
            model.RememberMe,
            lockoutOnFailure: true);

        if (result.Succeeded)
        {
            _logger.LogInformation("User {UserName} signed in", user.UserName);
            return RedirectToLocal(model.ReturnUrl);
        }

        if (result.IsLockedOut)
        {
            ModelState.AddModelError(string.Empty, "This account is locked. Please try again later.");
            return View(model);
        }

        ModelState.AddModelError(string.Empty, "Invalid username or password.");
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        _logger.LogInformation("User signed out");
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult AccessDenied()
    {
        return View();
    }

    private IActionResult RedirectToLocal(string? returnUrl)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction("Index", "Home");
    }
}
