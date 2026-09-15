using ToplandERP.Application.Abstractions;
using ToplandERP.Domain.Common;
using ToplandERP.Domain.Constants;

namespace ToplandERP.Infrastructure.Identity;

public sealed class SystemCurrentUser : ICurrentUser
{
    public static readonly SystemCurrentUser Instance = new();

    private SystemCurrentUser()
    {
    }

    public bool IsAuthenticated => false;

    public Guid? UserId => null;

    public Guid? CompanyId => null;

    public string? UserName => "system";

    public string? FullName => "System";

    public IReadOnlyCollection<string> Roles { get; } = [];

    public bool IsSuperAdmin => false;

    public bool BypassCompanyFilter => true;

    public bool CanAccessCompany(Guid companyId) => true;
}

public sealed class AnonymousCurrentUser : ICurrentUser
{
    public static readonly AnonymousCurrentUser Instance = new();

    private AnonymousCurrentUser()
    {
    }

    public bool IsAuthenticated => false;

    public Guid? UserId => null;

    public Guid? CompanyId => null;

    public string? UserName => null;

    public string? FullName => null;

    public IReadOnlyCollection<string> Roles { get; } = [];

    public bool IsSuperAdmin => false;

    public bool BypassCompanyFilter => false;

    public bool CanAccessCompany(Guid companyId) =>
        CompanyAccess.CanAccessCompany(IsSuperAdmin, CompanyId, companyId);
}
