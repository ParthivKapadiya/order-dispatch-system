using ToplandERP.Domain.Enums;

namespace ToplandERP.Application.Notifications;

public sealed class NotificationDto
{
    public required Guid Id { get; init; }

    public required Guid CompanyId { get; init; }

    public required NotificationType Type { get; init; }

    public required string Title { get; init; }

    public required string Message { get; init; }

    public bool IsRead { get; init; }

    public DateTime CreatedAt { get; init; }

    public DateTime? ReadAt { get; init; }

    public string? RelatedEntityType { get; init; }

    public Guid? RelatedEntityId { get; init; }

    public required string RelativeTime { get; init; }

    public string? NavigationUrl { get; init; }
}

public sealed class NotificationListQuery
{
    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 20;
}
