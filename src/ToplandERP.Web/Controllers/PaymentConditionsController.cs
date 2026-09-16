using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ToplandERP.Application.Abstractions;
using ToplandERP.Application.Common;
using ToplandERP.Application.Companies;
using ToplandERP.Application.PaymentConditions;
using ToplandERP.Domain.Constants;

namespace ToplandERP.Web.Controllers;

[Authorize(Policy = AuthorizationPolicies.CanViewPaymentConditions)]
public class PaymentConditionsController : AppController
{
    private readonly IPaymentConditionService _paymentConditionService;

    public PaymentConditionsController(
        IPaymentConditionService paymentConditionService,
        ICurrentUser currentUser,
        ICompanyService companyService)
        : base(currentUser, companyService)
    {
        _paymentConditionService = paymentConditionService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(PagedQuery query, CancellationToken cancellationToken)
    {
        SetPage("Payment conditions");
        ViewBag.Query = query;
        ViewBag.Companies = await GetAccessibleCompaniesAsync(cancellationToken);
        ViewBag.CanManage = User.IsInRole(RoleNames.SuperAdmin) || User.IsInRole(RoleNames.CompanyAdmin);
        return View(await _paymentConditionService.GetPagedAsync(query, cancellationToken));
    }

    [HttpGet]
    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        var item = await _paymentConditionService.GetByIdAsync(id, cancellationToken);
        if (item is null)
        {
            return NotFound();
        }

        SetPage("Payment condition details");
        ViewBag.CanManage = User.IsInRole(RoleNames.SuperAdmin) || User.IsInRole(RoleNames.CompanyAdmin);
        return View(item);
    }

    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.CanManagePaymentConditions)]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        SetPage("Create payment condition");
        await PrepareFormAsync(cancellationToken);
        return View(new PaymentConditionWriteRequest { CompanyId = CurrentUser.CompanyId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AuthorizationPolicies.CanManagePaymentConditions)]
    public async Task<IActionResult> Create(PaymentConditionWriteRequest request, CancellationToken cancellationToken)
    {
        SetPage("Create payment condition");
        await PrepareFormAsync(cancellationToken);
        if (!ModelState.IsValid)
        {
            return View(request);
        }

        try
        {
            await _paymentConditionService.CreateAsync(request, cancellationToken);
            TempData["Success"] = "Payment condition created.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception exception)
        {
            return HandleBusinessException(exception) ?? View(request);
        }
    }

    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.CanManagePaymentConditions)]
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        var item = await _paymentConditionService.GetByIdAsync(id, cancellationToken);
        if (item is null)
        {
            return NotFound();
        }

        SetPage("Edit payment condition");
        await PrepareFormAsync(cancellationToken);
        return View(new PaymentConditionWriteRequest
        {
            CompanyId = item.CompanyId,
            Name = item.Name,
            Description = item.Description
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AuthorizationPolicies.CanManagePaymentConditions)]
    public async Task<IActionResult> Edit(Guid id, PaymentConditionWriteRequest request, CancellationToken cancellationToken)
    {
        SetPage("Edit payment condition");
        await PrepareFormAsync(cancellationToken);
        if (!ModelState.IsValid)
        {
            return View(request);
        }

        try
        {
            await _paymentConditionService.UpdateAsync(id, request, cancellationToken);
            TempData["Success"] = "Payment condition updated.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (Exception exception)
        {
            return HandleBusinessException(exception) ?? View(request);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AuthorizationPolicies.CanManagePaymentConditions)]
    public async Task<IActionResult> SetActive(Guid id, bool isActive, CancellationToken cancellationToken)
    {
        try
        {
            await _paymentConditionService.SetActiveAsync(id, isActive, cancellationToken);
            TempData["Success"] = isActive ? "Payment condition activated." : "Payment condition deactivated.";
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
