using ToplandERP.Domain.Constants;
using ToplandERP.Domain.Enums;

namespace ToplandERP.Domain.Security;

public static class OrderWorkflowRules
{
    public static bool IsSalesEmployeeOnly(bool isSuperAdmin, IReadOnlyCollection<string> roles) =>
        !isSuperAdmin
        && roles.Contains(RoleNames.SalesEmployee)
        && !roles.Contains(RoleNames.CompanyAdmin)
        && !roles.Contains(RoleNames.DispatchUser);

    public static bool CanRequestModification(bool isSuperAdmin, IReadOnlyCollection<string> roles) =>
        IsSalesEmployeeOnly(isSuperAdmin, roles);

    public static bool CanReviewModification(bool isSuperAdmin, IReadOnlyCollection<string> roles) =>
        isSuperAdmin || roles.Contains(RoleNames.CompanyAdmin);

    public static bool CanMarkReadyToDispatch(bool isSuperAdmin, IReadOnlyCollection<string> roles) =>
        isSuperAdmin
        || roles.Contains(RoleNames.CompanyAdmin)
        || roles.Contains(RoleNames.DispatchUser);

    public static bool CanDispatch(bool isSuperAdmin, IReadOnlyCollection<string> roles) =>
        CanMarkReadyToDispatch(isSuperAdmin, roles);

    public static bool CanModifyOrderStatus(OrderStatus status) =>
        status is OrderStatus.Received or OrderStatus.ReadyToDispatch;

    public static bool IsLocked(OrderStatus status) =>
        status == OrderStatus.DispatchDone;
}
