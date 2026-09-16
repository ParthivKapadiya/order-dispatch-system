using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ToplandERP.Application.Abstractions;
using ToplandERP.Application.Common;
using ToplandERP.Application.Companies;
using ToplandERP.Domain.Constants;
using FluentValidation;

namespace ToplandERP.Web.Controllers;

[Authorize]
public abstract class AppController : Controller
{
    protected readonly ICurrentUser CurrentUser;
    protected readonly ICompanyService CompanyService;

    protected AppController(ICurrentUser currentUser, ICompanyService companyService)
    {
        CurrentUser = currentUser;
        CompanyService = companyService;
    }

    protected bool IsCompanyAdmin =>
        !CurrentUser.IsSuperAdmin && CurrentUser.Roles.Contains(RoleNames.CompanyAdmin);

    protected async Task<IReadOnlyList<CompanyDto>> GetAccessibleCompaniesAsync(CancellationToken cancellationToken)
    {
        return await CompanyService.GetAccessibleCompaniesAsync(cancellationToken);
    }

    protected IActionResult? HandleBusinessException(Exception exception)
    {
        switch (exception)
        {
            case ForbiddenException:
                return Forbid();
            case NotFoundException:
                return NotFound();
            case ValidationException validationException:
                foreach (var error in validationException.Errors)
                {
                    ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
                }

                return null;
            case BusinessException businessException:
                ModelState.AddModelError(string.Empty, businessException.Message);
                return null;
            default:
                throw exception;
        }
    }

    protected void SetPage(string title)
    {
        ViewData["Title"] = title;
        ViewData["Breadcrumb"] = title;
    }
}
