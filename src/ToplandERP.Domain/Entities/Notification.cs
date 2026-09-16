using ToplandERP.Domain.Common;
using ToplandERP.Domain.Enums;

namespace ToplandERP.Domain.Entities;

public class Notification : BaseEntity, ICompanyScoped
{
    public Guid CompanyId { get; set; }

    public Company Company { get; set; } = null!;

    public Guid RecipientUserId { get; set; }

    public NotificationType Type { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public string? RelatedEntityType { get; set; }

    public Guid? RelatedEntityId { get; set; }

    public string? EventKey { get; set; }

    public bool IsRead { get; set; }

    public DateTime? ReadAt { get; set; }
}
