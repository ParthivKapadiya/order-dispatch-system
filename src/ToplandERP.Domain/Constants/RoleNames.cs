namespace ToplandERP.Domain.Constants;

public static class RoleNames
{
    public const string SuperAdmin = "SuperAdmin";
    public const string CompanyAdmin = "CompanyAdmin";
    public const string SalesEmployee = "SalesEmployee";
    public const string DispatchUser = "DispatchUser";

    public static readonly IReadOnlyList<string> All =
    [
        SuperAdmin,
        CompanyAdmin,
        SalesEmployee,
        DispatchUser
    ];
}
