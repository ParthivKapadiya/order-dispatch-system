using ToplandERP.Domain.Common;

namespace ToplandERP.Domain.Entities;

public class PaymentCondition : BaseEntity, ICompanyScoped
{
    public Guid CompanyId { get; set; }

    public Company Company { get; set; } = null!;

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;
}
