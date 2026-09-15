using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using ToplandERP.Application.Abstractions;
using ToplandERP.Domain.Common;
using ToplandERP.Domain.Constants;

namespace ToplandERP.Web.Services;

public sealed class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private IReadOnlyCollection<string>? _roles;

    public CurrentUser(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated == true;

    public Guid? UserId => ParseGuid(User?.FindFirstValue(ClaimTypes.NameIdentifier));

    public Guid? CompanyId => ParseGuid(User?.FindFirstValue(AppClaimTypes.CompanyId));

    public string? UserName => User?.Identity?.Name;

    public string? FullName => User?.FindFirstValue(AppClaimTypes.FullName) ?? UserName;

    public IReadOnlyCollection<string> Roles
    {
        get
        {
            if (_roles is not null)
            {
                return _roles;
            }

            _roles = User?
                .FindAll(ClaimTypes.Role)
                .Select(claim => claim.Value)
                .ToArray() ?? [];

            return _roles;
        }
    }

    public bool IsSuperAdmin => Roles.Contains(RoleNames.SuperAdmin);

    public bool BypassCompanyFilter => false;

    public bool CanAccessCompany(Guid companyId) =>
        CompanyAccess.CanAccessCompany(IsSuperAdmin, CompanyId, companyId);

    private static Guid? ParseGuid(string? value)
    {
        return Guid.TryParse(value, out var parsed) ? parsed : null;
    }
}
