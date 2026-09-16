using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ToplandERP.Application.Abstractions;
using ToplandERP.Application.Companies;
using ToplandERP.Domain.Constants;

namespace ToplandERP.Web.Controllers;

[Authorize(Policy = AuthorizationPolicies.CanViewCompanies)]
public class CompaniesController : AppController
{
    public CompaniesController(ICurrentUser currentUser, ICompanyService companyService)
        : base(currentUser, companyService)
    {
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        SetPage("Companies");
        var companies = await GetAccessibleCompaniesAsync(cancellationToken);
        return View(companies);
    }

    [HttpGet]
    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        var company = await CompanyService.GetByIdAsync(id, cancellationToken);
        if (company is null)
        {
            return NotFound();
        }

        SetPage("Company details");
        return View(company);
    }
}
