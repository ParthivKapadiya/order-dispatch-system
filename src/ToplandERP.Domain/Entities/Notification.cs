using ToplandERP.Domain.Common;

namespace ToplandERP.Domain.Entities;

public class Notification : BaseEntity, ICompanyScoped
{
    public Guid CompanyId { get; set; }

    public Company Company { get; set; } = null!;

    public Guid? UserId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public bool IsRead { get; set; }

    public DateTime? ReadAt { get; set; }
}
