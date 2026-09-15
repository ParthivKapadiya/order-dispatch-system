using Microsoft.AspNetCore.Identity;
using ToplandERP.Domain.Entities;

namespace ToplandERP.Infrastructure.Identity;

public class ApplicationUser : IdentityUser<Guid>
{
    public Guid? CompanyId { get; set; }

    public Company? Company { get; set; }

    public string FullName { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
