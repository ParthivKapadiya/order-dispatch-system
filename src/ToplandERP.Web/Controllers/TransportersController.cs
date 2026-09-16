using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ToplandERP.Application.Abstractions;
using ToplandERP.Application.Common;
using ToplandERP.Application.Companies;
using ToplandERP.Application.Transporters;
using ToplandERP.Domain.Constants;

namespace ToplandERP.Web.Controllers;

[Authorize(Policy = AuthorizationPolicies.CanViewTransporters)]
public class TransportersController : AppController
{
    private readonly ITransporterService _transporterService;

    public TransportersController(
        ITransporterService transporterService,
        ICurrentUser currentUser,
        ICompanyService companyService)
        : base(currentUser, companyService)
    {
        _transporterService = transporterService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(PagedQuery query, CancellationToken cancellationToken)
    {
        SetPage("Transporters");
        ViewBag.Query = query;
        ViewBag.Companies = await GetAccessibleCompaniesAsync(cancellationToken);
        ViewBag.CanManage = User.IsInRole(RoleNames.SuperAdmin) || User.IsInRole(RoleNames.CompanyAdmin);
        return View(await _transporterService.GetPagedAsync(query, cancellationToken));
    }

    [HttpGet]
    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        var transporter = await _transporterService.GetByIdAsync(id, cancellationToken);
        if (transporter is null)
        {
            return NotFound();
        }

        SetPage("Transporter details");
        ViewBag.CanManage = User.IsInRole(RoleNames.SuperAdmin) || User.IsInRole(RoleNames.CompanyAdmin);
        return View(transporter);
    }

    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.CanManageTransporters)]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        SetPage("Create transporter");
        await PrepareFormAsync(cancellationToken);
        return View(new TransporterWriteRequest { CompanyId = CurrentUser.CompanyId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AuthorizationPolicies.CanManageTransporters)]
    public async Task<IActionResult> Create(TransporterWriteRequest request, CancellationToken cancellationToken)
    {
        SetPage("Create transporter");
        await PrepareFormAsync(cancellationToken);
        if (!ModelState.IsValid)
        {
            return View(request);
        }

        try
        {
            await _transporterService.CreateAsync(request, cancellationToken);
            TempData["Success"] = "Transporter created.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception exception)
        {
            return HandleBusinessException(exception) ?? View(request);
        }
    }

    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.CanManageTransporters)]
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        var transporter = await _transporterService.GetByIdAsync(id, cancellationToken);
        if (transporter is null)
        {
            return NotFound();
        }

        SetPage("Edit transporter");
        await PrepareFormAsync(cancellationToken);
        return View(new TransporterWriteRequest
        {
            CompanyId = transporter.CompanyId,
            Name = transporter.Name,
            ContactPerson = transporter.ContactPerson,
            Mobile = transporter.Mobile,
            Address = transporter.Address
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AuthorizationPolicies.CanManageTransporters)]
    public async Task<IActionResult> Edit(Guid id, TransporterWriteRequest request, CancellationToken cancellationToken)
    {
        SetPage("Edit transporter");
        await PrepareFormAsync(cancellationToken);
        if (!ModelState.IsValid)
        {
            return View(request);
        }

        try
        {
            await _transporterService.UpdateAsync(id, request, cancellationToken);
            TempData["Success"] = "Transporter updated.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (Exception exception)
        {
            return HandleBusinessException(exception) ?? View(request);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AuthorizationPolicies.CanManageTransporters)]
    public async Task<IActionResult> SetActive(Guid id, bool isActive, CancellationToken cancellationToken)
    {
        try
        {
            await _transporterService.SetActiveAsync(id, isActive, cancellationToken);
            TempData["Success"] = isActive ? "Transporter activated." : "Transporter deactivated.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (Exception exception)
        {
            return HandleBusinessException(exception) ?? RedirectToAction(nameof(Details), new { id });
        }
    }

    private async Task PrepareFormAsync(CancellationToken cancellationToken)
    {
        ViewBag.Companies = await GetAccessibleCompaniesAsync(cancellationToken);
        ViewBag.LockCompany = !CurrentUser.IsSuperAdmin;
    }
}
