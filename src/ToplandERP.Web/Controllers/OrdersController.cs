using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ToplandERP.Application.Abstractions;
using ToplandERP.Application.Companies;
using ToplandERP.Application.Orders;
using ToplandERP.Domain.Constants;

namespace ToplandERP.Web.Controllers;

[Authorize(Policy = AuthorizationPolicies.CanViewOrders)]
public class OrdersController : AppController
{
    private readonly IOrderService _orderService;

    public OrdersController(
        IOrderService orderService,
        ICurrentUser currentUser,
        ICompanyService companyService)
        : base(currentUser, companyService)
    {
        _orderService = orderService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(OrderListQuery query, CancellationToken cancellationToken)
    {
        SetPage("Orders");
        ViewBag.Query = query;
        ViewBag.Companies = await GetAccessibleCompaniesAsync(cancellationToken);
        ViewBag.CanCreate = User.IsInRole(RoleNames.SuperAdmin)
            || User.IsInRole(RoleNames.CompanyAdmin)
            || User.IsInRole(RoleNames.SalesEmployee);
        return View(await _orderService.GetPagedAsync(query, cancellationToken));
    }

    [HttpGet]
    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        var order = await _orderService.GetByIdAsync(id, cancellationToken);
        if (order is null)
        {
            return NotFound();
        }

        SetPage("Order details");
        ViewBag.CanRequestModification = User.IsInRole(RoleNames.SalesEmployee)
            && !User.IsInRole(RoleNames.CompanyAdmin)
            && !User.IsInRole(RoleNames.SuperAdmin)
            && order.CreatedByUserId == CurrentUser.UserId
            && !order.IsLocked
            && (order.Status == Domain.Enums.OrderStatus.Received || order.Status == Domain.Enums.OrderStatus.ReadyToDispatch);
        ViewBag.CanMarkReady = (User.IsInRole(RoleNames.SuperAdmin)
                || User.IsInRole(RoleNames.CompanyAdmin)
                || User.IsInRole(RoleNames.DispatchUser))
            && order.Status == Domain.Enums.OrderStatus.Received;
        ViewBag.CanOpenDispatch = (User.IsInRole(RoleNames.SuperAdmin)
                || User.IsInRole(RoleNames.CompanyAdmin)
                || User.IsInRole(RoleNames.DispatchUser))
            && order.Status == Domain.Enums.OrderStatus.ReadyToDispatch;
        return View(order);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AuthorizationPolicies.CanMarkReadyToDispatch)]
    public async Task<IActionResult> MarkReadyToDispatch(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var order = await _orderService.MarkReadyToDispatchAsync(id, cancellationToken);
            TempData["Success"] = $"Order {order.OrderNumber} is now Ready to Dispatch.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (Exception exception)
        {
            var handled = HandleBusinessException(exception);
            if (handled is not null)
            {
                return handled;
            }

            TempData["Error"] = ModelState.Values.SelectMany(item => item.Errors).FirstOrDefault()?.ErrorMessage
                ?? "Unable to update the order.";
            return RedirectToAction(nameof(Details), new { id });
        }
    }

    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.CanCreateOrders)]
    public async Task<IActionResult> Create(Guid? companyId, CancellationToken cancellationToken)
    {
        SetPage("Create order");
        var request = new CreateOrderRequest
        {
            CompanyId = CurrentUser.IsSuperAdmin ? companyId : CurrentUser.CompanyId,
            Items = [new OrderLineRequest(), new OrderLineRequest(), new OrderLineRequest()]
        };
        await PrepareCreateAsync(request, cancellationToken);
        return View(request);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AuthorizationPolicies.CanCreateOrders)]
    public async Task<IActionResult> Create(CreateOrderRequest request, CancellationToken cancellationToken)
    {
        SetPage("Create order");
        NormalizeItems(request);
        await PrepareCreateAsync(request, cancellationToken);
        if (!ModelState.IsValid)
        {
            return View(request);
        }

        try
        {
            var created = await _orderService.CreateAsync(request, cancellationToken);
            TempData["Success"] = $"Order {created.OrderNumber} created with status Order Received.";
            return RedirectToAction(nameof(Details), new { id = created.Id });
        }
        catch (Exception exception)
        {
            return HandleBusinessException(exception) ?? View(request);
        }
    }

    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.CanCreateOrders)]
    public async Task<IActionResult> CustomerLookup(Guid id, CancellationToken cancellationToken)
    {
        var customer = await _orderService.GetCustomerLookupAsync(id, cancellationToken);
        if (customer is null)
        {
            return NotFound();
        }

        return Json(customer);
    }

    private async Task PrepareCreateAsync(CreateOrderRequest request, CancellationToken cancellationToken)
    {
        ViewBag.Companies = await GetAccessibleCompaniesAsync(cancellationToken);
        ViewBag.LockCompany = !CurrentUser.IsSuperAdmin;
        ViewBag.Catalog = await _orderService.GetCreateCatalogAsync(request.CompanyId, cancellationToken);
        if (request.Items.Count < 1)
        {
            request.Items.Add(new OrderLineRequest());
        }
    }

    private static void NormalizeItems(CreateOrderRequest request)
    {
        request.Items ??= [];
        if (request.Items.Count == 0)
        {
            request.Items.Add(new OrderLineRequest());
        }
    }
}
