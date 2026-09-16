using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ToplandERP.Application.Abstractions;
using ToplandERP.Application.Companies;
using ToplandERP.Application.Dispatching;
using ToplandERP.Application.Orders;
using ToplandERP.Domain.Constants;
using ToplandERP.Domain.Enums;

namespace ToplandERP.Web.Controllers;

[Authorize]
public class DispatchController : AppController
{
    private readonly IDispatchService _dispatchService;
    private readonly IOrderService _orderService;

    public DispatchController(
        IDispatchService dispatchService,
        IOrderService orderService,
        ICurrentUser currentUser,
        ICompanyService companyService)
        : base(currentUser, companyService)
    {
        _dispatchService = dispatchService;
        _orderService = orderService;
    }

    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.CanDispatchOrders)]
    public async Task<IActionResult> Index(DispatchListQuery query, CancellationToken cancellationToken)
    {
        SetPage("Dispatch");
        ViewBag.Query = query;
        ViewBag.Companies = await GetAccessibleCompaniesAsync(cancellationToken);
        return View(await _dispatchService.GetReadyToDispatchAsync(query, cancellationToken));
    }

    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.CanDispatchOrders)]
    public async Task<IActionResult> Open(Guid orderId, CancellationToken cancellationToken)
    {
        var dispatch = await _dispatchService.GetByOrderIdAsync(orderId, cancellationToken);
        if (dispatch is null)
        {
            return NotFound();
        }

        SetPage("Dispatch order");
        ViewBag.Dispatch = dispatch;
        ViewBag.Catalog = await _orderService.GetCreateCatalogAsync(dispatch.CompanyId, cancellationToken);
        return View(new CompleteDispatchRequest
        {
            DispatchDate = DateTime.UtcNow.Date,
            DispatchPersonName = CurrentUser.FullName ?? CurrentUser.UserName ?? string.Empty,
            TransporterId = dispatch.TransporterId,
            LrNumber = dispatch.LrNumber,
            BookingNumber = dispatch.BookingNumber,
            Notes = dispatch.Notes
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AuthorizationPolicies.CanDispatchOrders)]
    public async Task<IActionResult> Complete(Guid orderId, CompleteDispatchRequest request, CancellationToken cancellationToken)
    {
        request.Files = await ReadUploadsAsync(cancellationToken);
        var dispatch = await _dispatchService.GetByOrderIdAsync(orderId, cancellationToken);
        if (dispatch is null)
        {
            return NotFound();
        }

        SetPage("Dispatch order");
        ViewBag.Dispatch = dispatch;
        ViewBag.Catalog = await _orderService.GetCreateCatalogAsync(dispatch.CompanyId, cancellationToken);
        if (!ModelState.IsValid)
        {
            return View("Open", request);
        }

        try
        {
            await _dispatchService.CompleteAsync(orderId, request, cancellationToken);
            TempData["Success"] = $"Order {dispatch.OrderNumber} marked Dispatch Done and locked.";
            return RedirectToAction("Details", "Orders", new { id = orderId });
        }
        catch (Exception exception)
        {
            return HandleBusinessException(exception) ?? View("Open", request);
        }
    }

    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.CanViewOrders)]
    public async Task<IActionResult> Document(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var file = await _dispatchService.OpenDocumentAsync(id, cancellationToken);
            return File(file.Stream, file.ContentType, file.FileName);
        }
        catch (Exception exception)
        {
            return HandleBusinessException(exception) ?? NotFound();
        }
    }

    private async Task<List<DispatchFileUpload>> ReadUploadsAsync(CancellationToken cancellationToken)
    {
        var files = new List<DispatchFileUpload>();
        await AddFilesAsync(files, "MaterialPhotos", DispatchDocumentType.MaterialPhoto, cancellationToken);
        await AddFilesAsync(files, "TransportReceipts", DispatchDocumentType.TransportReceipt, cancellationToken);
        await AddFilesAsync(files, "LrDocuments", DispatchDocumentType.LrDocument, cancellationToken);
        await AddFilesAsync(files, "OtherDocuments", DispatchDocumentType.Other, cancellationToken);
        return files;
    }

    private async Task AddFilesAsync(
        List<DispatchFileUpload> files,
        string formKey,
        DispatchDocumentType documentType,
        CancellationToken cancellationToken)
    {
        var uploads = Request.Form.Files.GetFiles(formKey);
        foreach (var upload in uploads)
        {
            if (upload.Length <= 0)
            {
                continue;
            }

            var stream = new MemoryStream();
            await upload.CopyToAsync(stream, cancellationToken);
            stream.Position = 0;
            files.Add(new DispatchFileUpload
            {
                Content = stream,
                OriginalFileName = upload.FileName,
                ContentType = upload.ContentType,
                DocumentType = documentType
            });
        }
    }
}
