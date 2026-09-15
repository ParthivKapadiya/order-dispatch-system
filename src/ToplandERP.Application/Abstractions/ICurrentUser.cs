namespace ToplandERP.Application.Abstractions;

public interface ICurrentUser
{
    bool IsAuthenticated { get; }

    Guid? UserId { get; }

    Guid? CompanyId { get; }

    string? UserName { get; }

    string? FullName { get; }

    IReadOnlyCollection<string> Roles { get; }

    bool IsSuperAdmin { get; }

    bool BypassCompanyFilter { get; }

    bool CanAccessCompany(Guid companyId);
}
