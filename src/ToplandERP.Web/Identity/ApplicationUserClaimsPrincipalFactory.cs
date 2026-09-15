using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using ToplandERP.Domain.Constants;
using ToplandERP.Infrastructure.Identity;

namespace ToplandERP.Web.Identity;

public sealed class ApplicationUserClaimsPrincipalFactory
    : UserClaimsPrincipalFactory<ApplicationUser, IdentityRole<Guid>>
{
    public ApplicationUserClaimsPrincipalFactory(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole<Guid>> roleManager,
        IOptions<IdentityOptions> options)
        : base(userManager, roleManager, options)
    {
    }

    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
    {
        var identity = await base.GenerateClaimsAsync(user);

        if (user.CompanyId.HasValue)
        {
            identity.AddClaim(new Claim(AppClaimTypes.CompanyId, user.CompanyId.Value.ToString()));
        }

        if (!string.IsNullOrWhiteSpace(user.FullName))
        {
            identity.AddClaim(new Claim(AppClaimTypes.FullName, user.FullName));
        }

        return identity;
    }
}
