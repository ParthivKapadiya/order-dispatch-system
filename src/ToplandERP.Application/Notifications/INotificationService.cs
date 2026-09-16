using ToplandERP.Application.Common;
using ToplandERP.Domain.Entities;

namespace ToplandERP.Application.Notifications;

public interface INotificationService
{
    Task NotifyOrderCreatedAsync(Order order, CancellationToken cancellationToken = default);

    Task NotifyOrderReadyToDispatchAsync(Order order, CancellationToken cancellationToken = default);

    Task NotifyOrderDispatchedAsync(
        Order order,
        string? transporterName,
        Guid? actorUserId,
        CancellationToken cancellationToken = default);

    Task NotifyModificationRequestedAsync(
        OrderModificationRequest request,
        Order order,
        CancellationToken cancellationToken = default);

    Task NotifyModificationApprovedAsync(
        OrderModificationRequest request,
        Order order,
        CancellationToken cancellationToken = default);

    Task NotifyModificationRejectedAsync(
        OrderModificationRequest request,
        Order order,
        CancellationToken cancellationToken = default);

    Task<PagedResult<NotificationDto>> GetUserNotificationsAsync(
        NotificationListQuery query,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<NotificationDto>> GetRecentAsync(
        int count = 8,
        CancellationToken cancellationToken = default);

    Task<int> GetUnreadCountAsync(CancellationToken cancellationToken = default);

    Task<NotificationDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<NotificationDto?> MarkAsReadAsync(Guid id, CancellationToken cancellationToken = default);

    Task<int> MarkAllAsReadAsync(CancellationToken cancellationToken = default);

    Task<string?> ResolveNavigationUrlAsync(
        NotificationDto notification,
        CancellationToken cancellationToken = default);
}
