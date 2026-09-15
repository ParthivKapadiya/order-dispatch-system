using ToplandERP.Application.Abstractions;
using ToplandERP.Domain.Common;
using ToplandERP.Domain.Constants;

namespace ToplandERP.UnitTests.Support;

public sealed class TestCurrentUser : ICurrentUser
{
    public bool IsAuthenticated { get; init; } = true;

    public Guid? UserId { get; init; } = Guid.NewGuid();

    public Guid? CompanyId { get; init; }

    public string? UserName { get; init; } = "tester";

    public string? FullName { get; init; } = "Test User";

    public IReadOnlyCollection<string> Roles { get; init; } = [];

    public bool IsSuperAdmin => Roles.Contains(RoleNames.SuperAdmin);

    public bool BypassCompanyFilter { get; init; }

    public bool CanAccessCompany(Guid companyId) =>
        CompanyAccess.CanAccessCompany(IsSuperAdmin, CompanyId, companyId);
}
