namespace ToplandERP.Domain.Constants;

public static class AuthorizationPolicies
{
    public const string SuperAdmin = "SuperAdmin";
    public const string CompanyAdmin = "CompanyAdmin";
    public const string SalesEmployee = "SalesEmployee";
    public const string DispatchUser = "DispatchUser";
    public const string AuthenticatedUser = "AuthenticatedUser";

    public const string CanViewCompanies = "CanViewCompanies";
    public const string CanViewCustomers = "CanViewCustomers";
    public const string CanManageCustomers = "CanManageCustomers";
    public const string CanEditCustomers = "CanEditCustomers";
    public const string CanToggleCustomers = "CanToggleCustomers";
    public const string CanViewProducts = "CanViewProducts";
    public const string CanManageProducts = "CanManageProducts";
    public const string CanViewTransporters = "CanViewTransporters";
    public const string CanManageTransporters = "CanManageTransporters";
    public const string CanViewPaymentConditions = "CanViewPaymentConditions";
    public const string CanManagePaymentConditions = "CanManagePaymentConditions";
    public const string CanViewUsers = "CanViewUsers";
    public const string CanManageUsers = "CanManageUsers";
    public const string CanViewOrders = "CanViewOrders";
    public const string CanCreateOrders = "CanCreateOrders";
    public const string CanRequestOrderModification = "CanRequestOrderModification";
    public const string CanReviewOrderModifications = "CanReviewOrderModifications";
    public const string CanMarkReadyToDispatch = "CanMarkReadyToDispatch";
    public const string CanDispatchOrders = "CanDispatchOrders";
}
