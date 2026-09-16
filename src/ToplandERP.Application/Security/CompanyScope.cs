using ToplandERP.Application.Abstractions;
using ToplandERP.Application.Common;
using ToplandERP.Domain.Common;

namespace ToplandERP.Application.Security;

public static class CompanyScope
{
    public static Guid ResolveWriteCompanyId(ICurrentUser currentUser, Guid? requestedCompanyId)
    {
        if (currentUser.IsSuperAdmin)
        {
            if (!requestedCompanyId.HasValue || requestedCompanyId.Value == Guid.Empty)
            {
                throw new BusinessException("Company is required.");
            }

            return requestedCompanyId.Value;
        }

        if (!currentUser.CompanyId.HasValue)
        {
            throw new ForbiddenException("Your account is not assigned to a company.");
        }

        return currentUser.CompanyId.Value;
    }

    public static IQueryable<T> ApplyOptionalCompanyFilter<T>(
        IQueryable<T> query,
        ICurrentUser currentUser,
        Guid? filterCompanyId)
        where T : ICompanyScoped
    {
        if (currentUser.IsSuperAdmin && filterCompanyId.HasValue && filterCompanyId.Value != Guid.Empty)
        {
            return query.Where(entity => entity.CompanyId == filterCompanyId.Value);
        }

        return query;
    }

    public static int NormalizePage(int page) => page < 1 ? 1 : page;

    public static int NormalizePageSize(int pageSize)
    {
        if (pageSize < 1)
        {
            return 20;
        }

        return Math.Min(pageSize, 100);
    }
}
