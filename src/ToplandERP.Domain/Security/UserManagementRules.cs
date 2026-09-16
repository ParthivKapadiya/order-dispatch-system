using ToplandERP.Domain.Constants;

namespace ToplandERP.Domain.Security;

public static class UserManagementRules
{
    public static IReadOnlyList<string> AssignableRoles(bool isSuperAdmin, bool isCompanyAdmin)
    {
        if (isSuperAdmin)
        {
            return
            [
                RoleNames.CompanyAdmin,
                RoleNames.SalesEmployee,
                RoleNames.DispatchUser
            ];
        }

        if (isCompanyAdmin)
        {
            return
            [
                RoleNames.SalesEmployee,
                RoleNames.DispatchUser
            ];
        }

        return [];
    }

    public static bool CanAssignRole(bool isSuperAdmin, bool isCompanyAdmin, string roleName)
    {
        return AssignableRoles(isSuperAdmin, isCompanyAdmin)
            .Contains(roleName, StringComparer.OrdinalIgnoreCase);
    }

    public static bool CanManageCompanyUsers(bool isSuperAdmin, Guid? actorCompanyId, Guid? targetCompanyId)
    {
        if (isSuperAdmin)
        {
            return true;
        }

        return actorCompanyId.HasValue
            && targetCompanyId.HasValue
            && actorCompanyId.Value == targetCompanyId.Value;
    }
}
