using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ToplandERP.Application.Abstractions;
using ToplandERP.Application.Companies;
using ToplandERP.Application.Notifications;
using ToplandERP.Domain.Constants;

namespace ToplandERP.Web.Controllers;

[Authorize(Policy = AuthorizationPolicies.AuthenticatedUser)]
public class NotificationsController : AppController
{
    private readonly INotificationService _notificationService;

    public NotificationsController(
        INotificationService notificationService,
        ICurrentUser currentUser,
        ICompanyService companyService)
        : base(currentUser, companyService)
    {
        _notificationService = notificationService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(NotificationListQuery query, CancellationToken cancellationToken)
    {
        SetPage("Notifications");
        ViewBag.Query = query;
        return View(await _notificationService.GetUserNotificationsAsync(query, cancellationToken));
    }

    [HttpGet]
    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        var notification = await _notificationService.GetByIdAsync(id, cancellationToken);
        if (notification is null)
        {
            return NotFound();
        }

        SetPage("Notification");
        return View(notification);
    }

    [HttpGet]
    public async Task<IActionResult> Recent(CancellationToken cancellationToken)
    {
        var items = await _notificationService.GetRecentAsync(NotificationService.RecentLimit, cancellationToken);
        return Json(items);
    }

    [HttpGet]
    public async Task<IActionResult> UnreadCount(CancellationToken cancellationToken)
    {
        var count = await _notificationService.GetUnreadCountAsync(cancellationToken);
        return Json(new { count });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAsRead(Guid id, CancellationToken cancellationToken)
    {
        var notification = await _notificationService.MarkAsReadAsync(id, cancellationToken);
        if (notification is null)
        {
            return NotFound();
        }

        TempData["Success"] = "Notification marked as read.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAllAsRead(CancellationToken cancellationToken)
    {
        await _notificationService.MarkAllAsReadAsync(cancellationToken);
        TempData["Success"] = "All notifications were marked as read.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Open(Guid id, CancellationToken cancellationToken)
    {
        var notification = await _notificationService.MarkAsReadAsync(id, cancellationToken);
        if (notification is null)
        {
            return NotFound();
        }

        var url = await _notificationService.ResolveNavigationUrlAsync(notification, cancellationToken);
        if (!string.IsNullOrWhiteSpace(url))
        {
            return Redirect(url);
        }

        return RedirectToAction(nameof(Details), new { id });
    }
}
