using ToplandERP.Domain.Common;

namespace ToplandERP.Domain.Entities;

public class OrderModificationRequest : BaseEntity, ICompanyScoped
{
    public Guid CompanyId { get; set; }

    public Guid OrderId { get; set; }

    public Order Order { get; set; } = null!;

    public Guid RequestedByUserId { get; set; }

    public string? Reason { get; set; }

    public string Status { get; set; } = string.Empty;
}
