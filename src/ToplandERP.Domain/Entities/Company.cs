using ToplandERP.Domain.Common;

namespace ToplandERP.Domain.Entities;

public class Company : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public string Code { get; set; } = string.Empty;

    public string? Address { get; set; }

    public string? Phone { get; set; }

    public string? Email { get; set; }

    public bool IsActive { get; set; } = true;
}
