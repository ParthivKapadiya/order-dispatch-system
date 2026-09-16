using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ToplandERP.Application.Abstractions;
using ToplandERP.Application.Common;
using ToplandERP.Application.Companies;
using ToplandERP.Application.Products;
using ToplandERP.Domain.Constants;

namespace ToplandERP.Web.Controllers;

[Authorize(Policy = AuthorizationPolicies.CanViewProducts)]
public class ProductsController : AppController
{
    private readonly IProductService _productService;

    public ProductsController(
        IProductService productService,
        ICurrentUser currentUser,
        ICompanyService companyService)
        : base(currentUser, companyService)
    {
        _productService = productService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(PagedQuery query, CancellationToken cancellationToken)
    {
        SetPage("Products");
        ViewBag.Query = query;
        ViewBag.Companies = await GetAccessibleCompaniesAsync(cancellationToken);
        ViewBag.CanManage = User.IsInRole(RoleNames.SuperAdmin) || User.IsInRole(RoleNames.CompanyAdmin);
        return View(await _productService.GetPagedAsync(query, cancellationToken));
    }

    [HttpGet]
    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        var product = await _productService.GetByIdAsync(id, cancellationToken);
        if (product is null)
        {
            return NotFound();
        }

        SetPage("Product details");
        ViewBag.CanManage = User.IsInRole(RoleNames.SuperAdmin) || User.IsInRole(RoleNames.CompanyAdmin);
        return View(product);
    }

    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.CanManageProducts)]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        SetPage("Create product");
        await PrepareFormAsync(cancellationToken);
        return View(new ProductWriteRequest { CompanyId = CurrentUser.CompanyId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AuthorizationPolicies.CanManageProducts)]
    public async Task<IActionResult> Create(ProductWriteRequest request, CancellationToken cancellationToken)
    {
        SetPage("Create product");
        await PrepareFormAsync(cancellationToken);
        if (!ModelState.IsValid)
        {
            return View(request);
        }

        try
        {
            await _productService.CreateAsync(request, cancellationToken);
            TempData["Success"] = "Product created.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception exception)
        {
            return HandleBusinessException(exception) ?? View(request);
        }
    }

    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.CanManageProducts)]
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        var product = await _productService.GetByIdAsync(id, cancellationToken);
        if (product is null)
        {
            return NotFound();
        }

        SetPage("Edit product");
        await PrepareFormAsync(cancellationToken);
        return View(new ProductWriteRequest
        {
            CompanyId = product.CompanyId,
            ProductCode = product.ProductCode,
            ProductName = product.ProductName,
            Category = product.Category,
            ModelNumber = product.ModelNumber,
            Description = product.Description,
            Unit = product.Unit
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AuthorizationPolicies.CanManageProducts)]
    public async Task<IActionResult> Edit(Guid id, ProductWriteRequest request, CancellationToken cancellationToken)
    {
        SetPage("Edit product");
        await PrepareFormAsync(cancellationToken);
        if (!ModelState.IsValid)
        {
            return View(request);
        }

        try
        {
            await _productService.UpdateAsync(id, request, cancellationToken);
            TempData["Success"] = "Product updated.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (Exception exception)
        {
            return HandleBusinessException(exception) ?? View(request);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AuthorizationPolicies.CanManageProducts)]
    public async Task<IActionResult> SetActive(Guid id, bool isActive, CancellationToken cancellationToken)
    {
        try
        {
            await _productService.SetActiveAsync(id, isActive, cancellationToken);
            TempData["Success"] = isActive ? "Product activated." : "Product deactivated.";
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
