using Microsoft.AspNetCore.Identity;
using ToplandERP.Application.Abstractions;

namespace ToplandERP.Infrastructure.Identity;

public sealed class IdentityUserDirectory : IUserDirectory
{
    private readonly UserManager<ApplicationUser> _userManager;

    public IdentityUserDirectory(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<IReadOnlyList<DirectoryUser>> GetActiveUsersInRoleAsync(
        string roleName,
        CancellationToken cancellationToken = default)
    {
        var users = await _userManager.GetUsersInRoleAsync(roleName);
        return users
            .Where(user => user.IsActive)
            .Select(user => new DirectoryUser(user.Id, user.CompanyId, user.FullName, user.IsActive))
            .ToList();
    }
}
