using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ToplandERP.Application.Abstractions;
using ToplandERP.Application.Common;
using ToplandERP.Application.Companies;
using ToplandERP.Application.Customers;
using ToplandERP.Domain.Constants;

namespace ToplandERP.Web.Controllers;

[Authorize(Policy = AuthorizationPolicies.CanViewCustomers)]
public class CustomersController : AppController
{
    private readonly ICustomerService _customerService;

    public CustomersController(
        ICustomerService customerService,
        ICurrentUser currentUser,
        ICompanyService companyService)
        : base(currentUser, companyService)
    {
        _customerService = customerService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(PagedQuery query, CancellationToken cancellationToken)
    {
        SetPage("Customers");
        ViewBag.Query = query;
        ViewBag.Companies = await GetAccessibleCompaniesAsync(cancellationToken);
        ViewBag.CanCreate = User.IsInRole(RoleNames.SuperAdmin)
            || User.IsInRole(RoleNames.CompanyAdmin)
            || User.IsInRole(RoleNames.SalesEmployee);
        ViewBag.CanEdit = User.IsInRole(RoleNames.SuperAdmin) || User.IsInRole(RoleNames.CompanyAdmin);
        var result = await _customerService.GetPagedAsync(query, cancellationToken);
        return View(result);
    }

    [HttpGet]
    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        var customer = await _customerService.GetByIdAsync(id, cancellationToken);
        if (customer is null)
        {
            return NotFound();
        }

        SetPage("Customer details");
        ViewBag.CanEdit = User.IsInRole(RoleNames.SuperAdmin) || User.IsInRole(RoleNames.CompanyAdmin);
        return View(customer);
    }

    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.CanManageCustomers)]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        SetPage("Create customer");
        await PrepareFormAsync(cancellationToken);
        return View(new CustomerWriteRequest { CompanyId = CurrentUser.CompanyId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AuthorizationPolicies.CanManageCustomers)]
    public async Task<IActionResult> Create(CustomerWriteRequest request, CancellationToken cancellationToken)
    {
        SetPage("Create customer");
        await PrepareFormAsync(cancellationToken);
        if (!ModelState.IsValid)
        {
            return View(request);
        }

        try
        {
            await _customerService.CreateAsync(request, cancellationToken);
            TempData["Success"] = "Customer created.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception exception)
        {
            var result = HandleBusinessException(exception);
            return result ?? View(request);
        }
    }

    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.CanEditCustomers)]
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        var customer = await _customerService.GetByIdAsync(id, cancellationToken);
        if (customer is null)
        {
            return NotFound();
        }

        SetPage("Edit customer");
        await PrepareFormAsync(cancellationToken);
        return View(ToRequest(customer));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AuthorizationPolicies.CanEditCustomers)]
    public async Task<IActionResult> Edit(Guid id, CustomerWriteRequest request, CancellationToken cancellationToken)
    {
        SetPage("Edit customer");
        await PrepareFormAsync(cancellationToken);
        if (!ModelState.IsValid)
        {
            return View(request);
        }

        try
        {
            await _customerService.UpdateAsync(id, request, cancellationToken);
            TempData["Success"] = "Customer updated.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (Exception exception)
        {
            var result = HandleBusinessException(exception);
            return result ?? View(request);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AuthorizationPolicies.CanToggleCustomers)]
    public async Task<IActionResult> SetActive(Guid id, bool isActive, CancellationToken cancellationToken)
    {
        try
        {
            await _customerService.SetActiveAsync(id, isActive, cancellationToken);
            TempData["Success"] = isActive ? "Customer activated." : "Customer deactivated.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (Exception exception)
        {
            var result = HandleBusinessException(exception);
            if (result is not null)
            {
                return result;
            }

            TempData["Error"] = "Unable to update customer status.";
            return RedirectToAction(nameof(Details), new { id });
        }
    }

    private async Task PrepareFormAsync(CancellationToken cancellationToken)
    {
        ViewBag.Companies = await GetAccessibleCompaniesAsync(cancellationToken);
        ViewBag.LockCompany = !CurrentUser.IsSuperAdmin;
    }

    private static CustomerWriteRequest ToRequest(CustomerDto customer)
    {
        return new CustomerWriteRequest
        {
            CompanyId = customer.CompanyId,
            CustomerCode = customer.CustomerCode,
            CustomerName = customer.CustomerName,
            Mobile = customer.Mobile,
            Email = customer.Email,
            BillingAddress = customer.BillingAddress,
            BillingCity = customer.BillingCity,
            BillingState = customer.BillingState,
            BillingPincode = customer.BillingPincode,
            DeliveryAddress = customer.DeliveryAddress,
            DeliveryCity = customer.DeliveryCity,
            DeliveryState = customer.DeliveryState,
            DeliveryPincode = customer.DeliveryPincode
        };
    }
}
