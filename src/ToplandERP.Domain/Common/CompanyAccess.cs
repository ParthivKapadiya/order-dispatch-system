namespace ToplandERP.Domain.Common;

public static class CompanyAccess
{
    public static bool CanAccessCompany(bool isSuperAdmin, Guid? userCompanyId, Guid targetCompanyId)
    {
        if (isSuperAdmin)
        {
            return true;
        }

        return userCompanyId.HasValue && userCompanyId.Value == targetCompanyId;
    }
}
