using Microsoft.EntityFrameworkCore;
using ToplandERP.Application.Abstractions;
using ToplandERP.Application.Common;
using ToplandERP.Application.Security;
using ToplandERP.Domain.Constants;
using ToplandERP.Domain.Entities;
using ToplandERP.Domain.Enums;

namespace ToplandERP.Application.Notifications;

public sealed class NotificationService : INotificationService
{
    public const int RecentLimit = 8;

    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IUserDirectory _userDirectory;

    public NotificationService(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        IUserDirectory userDirectory)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _userDirectory = userDirectory;
    }

    public Task NotifyOrderCreatedAsync(Order order, CancellationToken cancellationToken = default)
    {
        var copy = NotificationMessages.Compose(
            NotificationType.OrderCreated,
            order.OrderNumber ?? string.Empty,
            order.CustomerName);
        return CreateAsync(
            order.CompanyId,
            NotificationType.OrderCreated,
            copy.Title,
            copy.Message,
            NotificationRelatedEntities.Order,
            order.Id,
            ResolveManagementRecipientsAsync(order.CompanyId, includeDispatchUsers: false, cancellationToken),
            order.CreatedByUserId,
            cancellationToken);
    }

    public Task NotifyOrderReadyToDispatchAsync(Order order, CancellationToken cancellationToken = default)
    {
        var copy = NotificationMessages.Compose(
            NotificationType.OrderReadyToDispatch,
            order.OrderNumber ?? string.Empty);
        return CreateAsync(
            order.CompanyId,
            NotificationType.OrderReadyToDispatch,
            copy.Title,
            copy.Message,
            NotificationRelatedEntities.Order,
            order.Id,
            ResolveManagementRecipientsAsync(order.CompanyId, includeDispatchUsers: true, cancellationToken),
            excludeUserId: null,
            cancellationToken);
    }

    public Task NotifyOrderDispatchedAsync(
        Order order,
        string? transporterName,
        Guid? actorUserId,
        CancellationToken cancellationToken = default)
    {
        var copy = NotificationMessages.Compose(
            NotificationType.OrderDispatched,
            order.OrderNumber ?? string.Empty,
            transporterName: transporterName);
        return CreateAsync(
            order.CompanyId,
            NotificationType.OrderDispatched,
            copy.Title,
            copy.Message,
            NotificationRelatedEntities.Order,
            order.Id,
            ResolveDispatchDoneRecipientsAsync(order.CompanyId, order.CreatedByUserId, cancellationToken),
            actorUserId,
            cancellationToken);
    }

    public Task NotifyModificationRequestedAsync(
        OrderModificationRequest request,
        Order order,
        CancellationToken cancellationToken = default)
    {
        var copy = NotificationMessages.Compose(
            NotificationType.ModificationRequested,
            order.OrderNumber ?? string.Empty,
            actorName: request.RequestedByName);
        return CreateAsync(
            order.CompanyId,
            NotificationType.ModificationRequested,
            copy.Title,
            copy.Message,
            NotificationRelatedEntities.OrderModificationRequest,
            request.Id,
            ResolveManagementRecipientsAsync(order.CompanyId, includeDispatchUsers: false, cancellationToken),
            request.RequestedByUserId,
            cancellationToken);
    }

    public Task NotifyModificationApprovedAsync(
        OrderModificationRequest request,
        Order order,
        CancellationToken cancellationToken = default)
    {
        var copy = NotificationMessages.Compose(
            NotificationType.ModificationApproved,
            order.OrderNumber ?? string.Empty);
        return CreateAsync(
            order.CompanyId,
            NotificationType.ModificationApproved,
            copy.Title,
            copy.Message,
            NotificationRelatedEntities.OrderModificationRequest,
            request.Id,
            Task.FromResult<IReadOnlyCollection<Guid>>(
                request.RequestedByUserId == Guid.Empty ? [] : [request.RequestedByUserId]),
            _currentUser.UserId,
            cancellationToken);
    }

    public Task NotifyModificationRejectedAsync(
        OrderModificationRequest request,
        Order order,
        CancellationToken cancellationToken = default)
    {
        var copy = NotificationMessages.Compose(
            NotificationType.ModificationRejected,
            order.OrderNumber ?? string.Empty,
            rejectionReason: request.RejectionReason);
        return CreateAsync(
            order.CompanyId,
            NotificationType.ModificationRejected,
            copy.Title,
            copy.Message,
            NotificationRelatedEntities.OrderModificationRequest,
            request.Id,
            Task.FromResult<IReadOnlyCollection<Guid>>(
                request.RequestedByUserId == Guid.Empty ? [] : [request.RequestedByUserId]),
            _currentUser.UserId,
            cancellationToken);
    }

    public async Task<PagedResult<NotificationDto>> GetUserNotificationsAsync(
        NotificationListQuery query,
        CancellationToken cancellationToken = default)
    {
        var userId = RequireRecipient();
        var page = CompanyScope.NormalizePage(query.Page);
        var pageSize = CompanyScope.NormalizePageSize(query.PageSize);
        var dbQuery = InboxQuery(userId);
        var total = await dbQuery.CountAsync(cancellationToken);
        var items = await dbQuery
            .OrderByDescending(item => item.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<NotificationDto>
        {
            Items = items.Select(ToDto).ToList(),
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<IReadOnlyList<NotificationDto>> GetRecentAsync(
        int count = RecentLimit,
        CancellationToken cancellationToken = default)
    {
        var userId = RequireRecipient();
        var take = count <= 0 ? RecentLimit : Math.Min(count, 20);
        var items = await InboxQuery(userId)
            .OrderByDescending(item => item.CreatedAt)
            .Take(take)
            .ToListAsync(cancellationToken);
        return items.Select(ToDto).ToList();
    }

    public async Task<int> GetUnreadCountAsync(CancellationToken cancellationToken = default)
    {
        var userId = RequireRecipient();
        return await InboxQuery(userId).CountAsync(item => !item.IsRead, cancellationToken);
    }

    public async Task<NotificationDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await FindOwnedAsync(id, cancellationToken);
        return entity is null ? null : ToDto(entity);
    }

    public async Task<NotificationDto?> MarkAsReadAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await FindOwnedAsync(id, cancellationToken);
        if (entity is null)
        {
            return null;
        }

        if (!entity.IsRead)
        {
            entity.IsRead = true;
            entity.ReadAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return ToDto(entity);
    }

    public async Task<int> MarkAllAsReadAsync(CancellationToken cancellationToken = default)
    {
        var userId = RequireRecipient();
        var unread = await _dbContext.Notifications
            .Where(item => item.RecipientUserId == userId && !item.IsRead)
            .ToListAsync(cancellationToken);

        if (unread.Count == 0)
        {
            return 0;
        }

        var readAt = DateTime.UtcNow;
        foreach (var item in unread)
        {
            item.IsRead = true;
            item.ReadAt = readAt;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return unread.Count;
    }

    public async Task<string?> ResolveNavigationUrlAsync(
        NotificationDto notification,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(notification.RelatedEntityType) || !notification.RelatedEntityId.HasValue)
        {
            return null;
        }

        var relatedId = notification.RelatedEntityId.Value;
        if (notification.RelatedEntityType == NotificationRelatedEntities.Order)
        {
            var exists = await _dbContext.Orders.AsNoTracking()
                .AnyAsync(item => item.Id == relatedId, cancellationToken);
            return exists ? BuildNavigationUrl(notification.RelatedEntityType, relatedId) : null;
        }

        if (notification.RelatedEntityType == NotificationRelatedEntities.OrderModificationRequest)
        {
            var exists = await _dbContext.OrderModificationRequests.AsNoTracking()
                .AnyAsync(item => item.Id == relatedId, cancellationToken);
            return exists ? BuildNavigationUrl(notification.RelatedEntityType, relatedId) : null;
        }

        return null;
    }

    private async Task CreateAsync(
        Guid companyId,
        NotificationType type,
        string title,
        string message,
        string relatedEntityType,
        Guid relatedEntityId,
        Task<IReadOnlyCollection<Guid>> recipientsTask,
        Guid? excludeUserId,
        CancellationToken cancellationToken)
    {
        var recipients = (await recipientsTask)
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToList();

        if (excludeUserId.HasValue)
        {
            recipients.Remove(excludeUserId.Value);
        }

        if (recipients.Count == 0)
        {
            return;
        }

        var keys = recipients
            .Select(id => BuildEventKey(type, relatedEntityId, id))
            .ToList();
        var existing = await _dbContext.Notifications
            .IgnoreQueryFilters()
            .Where(item => item.EventKey != null && keys.Contains(item.EventKey))
            .Select(item => item.EventKey!)
            .ToListAsync(cancellationToken);
        var existingKeys = existing.ToHashSet(StringComparer.Ordinal);
        foreach (var tracked in _dbContext.Notifications.Local)
        {
            if (!string.IsNullOrWhiteSpace(tracked.EventKey))
            {
                existingKeys.Add(tracked.EventKey);
            }
        }

        foreach (var recipientId in recipients)
        {
            var eventKey = BuildEventKey(type, relatedEntityId, recipientId);
            if (!existingKeys.Add(eventKey))
            {
                continue;
            }

            _dbContext.Notifications.Add(new Notification
            {
                CompanyId = companyId,
                RecipientUserId = recipientId,
                Type = type,
                Title = title,
                Message = message,
                RelatedEntityType = relatedEntityType,
                RelatedEntityId = relatedEntityId,
                EventKey = eventKey,
                IsRead = false
            });
        }
    }

    private async Task<IReadOnlyCollection<Guid>> ResolveManagementRecipientsAsync(
        Guid companyId,
        bool includeDispatchUsers,
        CancellationToken cancellationToken)
    {
        var recipients = new HashSet<Guid>();
        await AddRoleAsync(recipients, RoleNames.CompanyAdmin, companyId, cancellationToken);
        await AddRoleAsync(recipients, RoleNames.SuperAdmin, companyId: null, cancellationToken);
        if (includeDispatchUsers)
        {
            await AddRoleAsync(recipients, RoleNames.DispatchUser, companyId, cancellationToken);
        }

        return recipients;
    }

    private async Task<IReadOnlyCollection<Guid>> ResolveDispatchDoneRecipientsAsync(
        Guid companyId,
        Guid? orderCreatorUserId,
        CancellationToken cancellationToken)
    {
        var recipients = new HashSet<Guid>();
        await AddRoleAsync(recipients, RoleNames.CompanyAdmin, companyId, cancellationToken);
        await AddRoleAsync(recipients, RoleNames.SuperAdmin, companyId: null, cancellationToken);
        if (orderCreatorUserId.HasValue && orderCreatorUserId.Value != Guid.Empty)
        {
            recipients.Add(orderCreatorUserId.Value);
        }

        return recipients;
    }

    private async Task AddRoleAsync(
        HashSet<Guid> recipients,
        string roleName,
        Guid? companyId,
        CancellationToken cancellationToken)
    {
        var users = await _userDirectory.GetActiveUsersInRoleAsync(roleName, cancellationToken);
        foreach (var user in users)
        {
            if (!user.IsActive)
            {
                continue;
            }

            if (companyId.HasValue && user.CompanyId != companyId.Value)
            {
                continue;
            }

            recipients.Add(user.UserId);
        }
    }

    private IQueryable<Notification> InboxQuery(Guid userId) =>
        _dbContext.Notifications.AsNoTracking().Where(item => item.RecipientUserId == userId);

    private async Task<Notification?> FindOwnedAsync(Guid id, CancellationToken cancellationToken)
    {
        var userId = RequireRecipient();
        return await _dbContext.Notifications
            .FirstOrDefaultAsync(item => item.Id == id && item.RecipientUserId == userId, cancellationToken);
    }

    private Guid RequireRecipient()
    {
        if (!_currentUser.IsAuthenticated || !_currentUser.UserId.HasValue)
        {
            throw new ForbiddenException("You do not have permission to perform this action.");
        }

        return _currentUser.UserId.Value;
    }

    private static string BuildEventKey(NotificationType type, Guid relatedEntityId, Guid recipientUserId) =>
        $"{(int)type}:{relatedEntityId:N}:{recipientUserId:N}";

    private static NotificationDto ToDto(Notification entity)
    {
        return new NotificationDto
        {
            Id = entity.Id,
            CompanyId = entity.CompanyId,
            Type = entity.Type,
            Title = entity.Title,
            Message = entity.Message,
            IsRead = entity.IsRead,
            CreatedAt = entity.CreatedAt,
            ReadAt = entity.ReadAt,
            RelatedEntityType = entity.RelatedEntityType,
            RelatedEntityId = entity.RelatedEntityId,
            RelativeTime = RelativeTimeFormatter.ToRelative(entity.CreatedAt),
            NavigationUrl = BuildNavigationUrl(entity.RelatedEntityType, entity.RelatedEntityId)
        };
    }

    private static string? BuildNavigationUrl(string? relatedEntityType, Guid? relatedEntityId)
    {
        if (string.IsNullOrWhiteSpace(relatedEntityType) || !relatedEntityId.HasValue)
        {
            return null;
        }

        return relatedEntityType switch
        {
            NotificationRelatedEntities.Order => $"/Orders/Details/{relatedEntityId.Value}",
            NotificationRelatedEntities.OrderModificationRequest => $"/ModificationRequests/Details/{relatedEntityId.Value}",
            _ => null
        };
    }
}
