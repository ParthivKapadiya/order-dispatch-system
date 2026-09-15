using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ToplandERP.Application.Abstractions;
using ToplandERP.Application.Companies;

namespace ToplandERP.Web.Controllers;

[Authorize]
public class HomeController : Controller
{
    private readonly ICompanyService _companyService;
    private readonly ICurrentUser _currentUser;

    public HomeController(ICompanyService companyService, ICurrentUser currentUser)
    {
        _companyService = companyService;
        _currentUser = currentUser;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Dashboard";
        ViewData["Breadcrumb"] = "Dashboard";
        ViewBag.FullName = _currentUser.FullName;
        ViewBag.IsSuperAdmin = _currentUser.IsSuperAdmin;
        ViewBag.Companies = await _companyService.GetAccessibleCompaniesAsync(cancellationToken);
        ViewBag.Roles = _currentUser.Roles;
        return View();
    }
}
