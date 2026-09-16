using ToplandERP.Domain.Common;
using ToplandERP.Domain.Enums;

namespace ToplandERP.Domain.Entities;

public class OrderModificationRequest : BaseEntity, ICompanyScoped
{
    public Guid CompanyId { get; set; }

    public Guid OrderId { get; set; }

    public Order Order { get; set; } = null!;

    public Guid RequestedByUserId { get; set; }

    public string RequestedByName { get; set; } = string.Empty;

    public string? Reason { get; set; }

    public string CurrentSnapshotJson { get; set; } = string.Empty;

    public string RequestedChangesJson { get; set; } = string.Empty;

    public ModificationRequestStatus Status { get; set; } = ModificationRequestStatus.Pending;

    public Guid? ReviewedByUserId { get; set; }

    public string? ReviewedByName { get; set; }

    public DateTime? ReviewedAt { get; set; }

    public string? RejectionReason { get; set; }
}
