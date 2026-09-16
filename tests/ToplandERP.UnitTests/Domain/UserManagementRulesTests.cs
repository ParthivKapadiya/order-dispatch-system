using FluentAssertions;
using ToplandERP.Domain.Constants;
using ToplandERP.Domain.Security;

namespace ToplandERP.UnitTests.Domain;

public class UserManagementRulesTests
{
    [Fact]
    public void CompanyAdmin_cannot_assign_SuperAdmin()
    {
        UserManagementRules.CanAssignRole(isSuperAdmin: false, isCompanyAdmin: true, RoleNames.SuperAdmin)
            .Should().BeFalse();
    }

    [Fact]
    public void CompanyAdmin_can_assign_operational_roles_only()
    {
        UserManagementRules.AssignableRoles(isSuperAdmin: false, isCompanyAdmin: true)
            .Should().BeEquivalentTo(RoleNames.SalesEmployee, RoleNames.DispatchUser);
    }

    [Fact]
    public void SuperAdmin_cannot_assign_SuperAdmin_from_user_management()
    {
        UserManagementRules.CanAssignRole(isSuperAdmin: true, isCompanyAdmin: false, RoleNames.SuperAdmin)
            .Should().BeFalse();
    }

    [Fact]
    public void CompanyAdmin_cannot_manage_another_company_user()
    {
        UserManagementRules.CanManageCompanyUsers(false, SeedIdentifiers.GravisCompanyId, SeedIdentifiers.JeekoCompanyId)
            .Should().BeFalse();
    }

    [Fact]
    public void SuperAdmin_can_manage_any_company_user()
    {
        UserManagementRules.CanManageCompanyUsers(true, null, SeedIdentifiers.ShreeCompanyId)
            .Should().BeTrue();
    }
}
