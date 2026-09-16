using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ToplandERP.Application.Abstractions;
using ToplandERP.Application.Companies;
using ToplandERP.Application.Users;
using ToplandERP.Domain.Constants;

namespace ToplandERP.Web.Controllers;

[Authorize(Policy = AuthorizationPolicies.CanViewUsers)]
public class UsersController : AppController
{
    private readonly IUserService _userService;

    public UsersController(
        IUserService userService,
        ICurrentUser currentUser,
        ICompanyService companyService)
        : base(currentUser, companyService)
    {
        _userService = userService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(UserListQuery query, CancellationToken cancellationToken)
    {
        SetPage("Employees");
        ViewBag.Query = query;
        ViewBag.Companies = await GetAccessibleCompaniesAsync(cancellationToken);
        ViewBag.Roles = _userService.GetAssignableRoles();
        ViewBag.CanManage = User.IsInRole(RoleNames.SuperAdmin) || User.IsInRole(RoleNames.CompanyAdmin);
        return View(await _userService.GetPagedAsync(query, cancellationToken));
    }

    [HttpGet]
    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        var user = await _userService.GetByIdAsync(id, cancellationToken);
        if (user is null)
        {
            return NotFound();
        }

        SetPage("Employee details");
        return View(user);
    }

    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.CanManageUsers)]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        SetPage("Create employee");
        await PrepareFormAsync(cancellationToken);
        return View(new CreateUserRequest { CompanyId = CurrentUser.CompanyId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AuthorizationPolicies.CanManageUsers)]
    public async Task<IActionResult> Create(CreateUserRequest request, CancellationToken cancellationToken)
    {
        SetPage("Create employee");
        await PrepareFormAsync(cancellationToken);
        if (!ModelState.IsValid)
        {
            return View(request);
        }

        try
        {
            await _userService.CreateAsync(request, cancellationToken);
            TempData["Success"] = "Employee created.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception exception)
        {
            return HandleBusinessException(exception) ?? View(request);
        }
    }

    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.CanManageUsers)]
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        var user = await _userService.GetByIdAsync(id, cancellationToken);
        if (user is null)
        {
            return NotFound();
        }

        SetPage("Edit employee");
        await PrepareFormAsync(cancellationToken);
        return View(new UpdateUserRequest
        {
            FullName = user.FullName,
            EmployeeCode = user.EmployeeCode,
            Email = user.Email,
            Mobile = user.Mobile,
            Role = user.Role
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AuthorizationPolicies.CanManageUsers)]
    public async Task<IActionResult> Edit(Guid id, UpdateUserRequest request, CancellationToken cancellationToken)
    {
        SetPage("Edit employee");
        await PrepareFormAsync(cancellationToken);
        if (!ModelState.IsValid)
        {
            return View(request);
        }

        try
        {
            await _userService.UpdateAsync(id, request, cancellationToken);
            TempData["Success"] = "Employee updated.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (Exception exception)
        {
            return HandleBusinessException(exception) ?? View(request);
        }
    }

    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.CanManageUsers)]
    public async Task<IActionResult> ResetPassword(Guid id, CancellationToken cancellationToken)
    {
        var user = await _userService.GetByIdAsync(id, cancellationToken);
        if (user is null)
        {
            return NotFound();
        }

        SetPage("Reset password");
        ViewBag.User = user;
        return View(new ResetUserPasswordRequest());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AuthorizationPolicies.CanManageUsers)]
    public async Task<IActionResult> ResetPassword(Guid id, ResetUserPasswordRequest request, CancellationToken cancellationToken)
    {
        var user = await _userService.GetByIdAsync(id, cancellationToken);
        if (user is null)
        {
            return NotFound();
        }

        SetPage("Reset password");
        ViewBag.User = user;
        if (!ModelState.IsValid)
        {
            return View(request);
        }

        try
        {
            await _userService.ResetPasswordAsync(id, request, cancellationToken);
            TempData["Success"] = "Password was reset. The user must sign in again.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (Exception exception)
        {
            return HandleBusinessException(exception) ?? View(request);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AuthorizationPolicies.CanManageUsers)]
    public async Task<IActionResult> SetActive(Guid id, bool isActive, CancellationToken cancellationToken)
    {
        try
        {
            await _userService.SetActiveAsync(id, isActive, cancellationToken);
            TempData["Success"] = isActive ? "Employee activated." : "Employee deactivated.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (Exception exception)
        {
            var result = HandleBusinessException(exception);
            if (result is not null)
            {
                return result;
            }

            TempData["Error"] = ModelState.Values.SelectMany(item => item.Errors).FirstOrDefault()?.ErrorMessage
                ?? "Unable to update employee status.";
            return RedirectToAction(nameof(Details), new { id });
        }
    }

    private async Task PrepareFormAsync(CancellationToken cancellationToken)
    {
        ViewBag.Companies = await GetAccessibleCompaniesAsync(cancellationToken);
        ViewBag.Roles = _userService.GetAssignableRoles();
        ViewBag.LockCompany = !CurrentUser.IsSuperAdmin;
    }
}
