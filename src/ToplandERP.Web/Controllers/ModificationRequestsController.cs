using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ToplandERP.Application.Abstractions;
using ToplandERP.Application.Companies;
using ToplandERP.Application.Modifications;
using ToplandERP.Application.Orders;
using ToplandERP.Domain.Constants;

namespace ToplandERP.Web.Controllers;

[Authorize]
public class ModificationRequestsController : AppController
{
    private readonly IModificationService _modificationService;
    private readonly IOrderService _orderService;

    public ModificationRequestsController(
        IModificationService modificationService,
        IOrderService orderService,
        ICurrentUser currentUser,
        ICompanyService companyService)
        : base(currentUser, companyService)
    {
        _modificationService = modificationService;
        _orderService = orderService;
    }

    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.CanReviewOrderModifications)]
    public async Task<IActionResult> Index(ModificationListQuery query, CancellationToken cancellationToken)
    {
        SetPage("Modification requests");
        ViewBag.Query = query;
        ViewBag.Companies = await GetAccessibleCompaniesAsync(cancellationToken);
        return View(await _modificationService.GetPagedAsync(query, cancellationToken));
    }

    [HttpGet]
    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        var request = await _modificationService.GetByIdAsync(id, cancellationToken);
        if (request is null)
        {
            return NotFound();
        }

        SetPage("Modification request");
        ViewBag.CanReview = User.IsInRole(RoleNames.SuperAdmin) || User.IsInRole(RoleNames.CompanyAdmin);
        return View(request);
    }

    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.CanRequestOrderModification)]
    public async Task<IActionResult> Create(Guid orderId, CancellationToken cancellationToken)
    {
        var order = await _orderService.GetByIdAsync(orderId, cancellationToken);
        if (order is null)
        {
            return NotFound();
        }

        SetPage("Request modification");
        await PrepareFormAsync(order, cancellationToken);
        return View(FromOrder(order));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AuthorizationPolicies.CanRequestOrderModification)]
    public async Task<IActionResult> Create(Guid orderId, CreateModificationRequest request, CancellationToken cancellationToken)
    {
        var order = await _orderService.GetByIdAsync(orderId, cancellationToken);
        if (order is null)
        {
            return NotFound();
        }

        SetPage("Request modification");
        request.Items ??= [];
        if (request.Items.Count == 0)
        {
            request.Items.Add(new OrderLineRequest());
        }

        await PrepareFormAsync(order, cancellationToken);
        if (!ModelState.IsValid)
        {
            return View(request);
        }

        try
        {
            await _modificationService.RequestAsync(orderId, request, cancellationToken);
            TempData["Success"] = "Modification request submitted and awaiting approval.";
            return RedirectToAction("Details", "Orders", new { id = orderId });
        }
        catch (Exception exception)
        {
            return HandleBusinessException(exception) ?? View(request);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AuthorizationPolicies.CanReviewOrderModifications)]
    public async Task<IActionResult> Approve(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var request = await _modificationService.ApproveAsync(id, cancellationToken);
            TempData["Success"] = $"Modification for {request.OrderNumber} approved. The order was updated.";
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
                ?? "Unable to approve the request.";
            return RedirectToAction(nameof(Details), new { id });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AuthorizationPolicies.CanReviewOrderModifications)]
    public async Task<IActionResult> Reject(Guid id, RejectModificationRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            var existing = await _modificationService.GetByIdAsync(id, cancellationToken);
            if (existing is null)
            {
                return NotFound();
            }

            SetPage("Modification request");
            ViewBag.CanReview = true;
            ViewBag.RejectRequest = request;
            return View("Details", existing);
        }

        try
        {
            var rejected = await _modificationService.RejectAsync(id, request, cancellationToken);
            TempData["Success"] = $"Modification for {rejected.OrderNumber} rejected. The order was not changed.";
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
                ?? "Unable to reject the request.";
            return RedirectToAction(nameof(Details), new { id });
        }
    }

    private async Task PrepareFormAsync(OrderDto order, CancellationToken cancellationToken)
    {
        ViewBag.Order = order;
        ViewBag.Catalog = await _orderService.GetCreateCatalogAsync(order.CompanyId, cancellationToken);
    }

    private static CreateModificationRequest FromOrder(OrderDto order)
    {
        return new CreateModificationRequest
        {
            BillAmount = order.BillAmount,
            PaymentConditionId = order.PaymentConditionId,
            TransporterId = order.TransporterId,
            BillingAddress = order.BillingAddress,
            BillingCity = order.BillingCity,
            BillingState = order.BillingState,
            BillingPincode = order.BillingPincode,
            DeliveryAddress = order.DeliveryAddress,
            DeliveryCity = order.DeliveryCity,
            DeliveryState = order.DeliveryState,
            DeliveryPincode = order.DeliveryPincode,
            BookingNumber = order.BookingNumber,
            BookingDate = order.BookingDate,
            BookingFrom = order.BookingFrom,
            BookingTo = order.BookingTo,
            BookingDetails = order.BookingDetails,
            Remarks = order.Remarks,
            SpecialInstructions = order.SpecialInstructions,
            Items = order.Items.Select(item => new OrderLineRequest
            {
                ProductId = item.ProductId,
                Quantity = item.Quantity
            }).ToList()
        };
    }
}
