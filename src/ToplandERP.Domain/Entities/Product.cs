using ToplandERP.Domain.Common;

namespace ToplandERP.Domain.Entities;

public class Product : BaseEntity, ICompanyScoped
{
    public Guid CompanyId { get; set; }

    public Company Company { get; set; } = null!;

    public string ProductCode { get; set; } = string.Empty;

    public string ProductName { get; set; } = string.Empty;

    public string? Category { get; set; }

    public string? ModelNumber { get; set; }

    public string? Description { get; set; }

    public string? Unit { get; set; }

    public bool IsActive { get; set; } = true;
}
