using ToplandERP.Application.Abstractions;
using ToplandERP.Domain.Constants;

namespace ToplandERP.UnitTests.Support;

public sealed class TestUserDirectory : IUserDirectory
{
    public List<DirectoryUserRecord> Users { get; } = [];

    public Task<IReadOnlyList<DirectoryUser>> GetActiveUsersInRoleAsync(
        string roleName,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<DirectoryUser> matches = Users
            .Where(user => user.IsActive && string.Equals(user.Role, roleName, StringComparison.Ordinal))
            .Select(user => new DirectoryUser(user.UserId, user.CompanyId, user.FullName, user.IsActive))
            .ToList();
        return Task.FromResult(matches);
    }

    public TestUserDirectory With(
        Guid userId,
        string role,
        Guid? companyId,
        string fullName = "User",
        bool isActive = true)
    {
        Users.Add(new DirectoryUserRecord(userId, companyId, role, fullName, isActive));
        return this;
    }

    public static TestUserDirectory Standard(
        Guid superAdminId,
        Guid companyAdminId,
        Guid dispatchUserId,
        Guid? salesUserId = null,
        Guid? companyId = null)
    {
        var scopedCompanyId = companyId ?? SeedIdentifiers.GravisCompanyId;
        var directory = new TestUserDirectory()
            .With(superAdminId, RoleNames.SuperAdmin, null, "Super Admin")
            .With(companyAdminId, RoleNames.CompanyAdmin, scopedCompanyId, "Company Admin")
            .With(dispatchUserId, RoleNames.DispatchUser, scopedCompanyId, "Dispatch User");
        if (salesUserId.HasValue)
        {
            directory.With(salesUserId.Value, RoleNames.SalesEmployee, scopedCompanyId, "Sales Employee");
        }

        return directory;
    }
}

public sealed record DirectoryUserRecord(
    Guid UserId,
    Guid? CompanyId,
    string Role,
    string FullName,
    bool IsActive = true);
