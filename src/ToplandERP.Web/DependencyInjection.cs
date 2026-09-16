using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Identity;
using ToplandERP.Application.Abstractions;
using ToplandERP.Domain.Constants;
using ToplandERP.Infrastructure.Identity;
using ToplandERP.Web.Identity;
using ToplandERP.Web.Services;
using ToplandERP.Web.ViewModels.Account;

namespace ToplandERP.Web;

public static class DependencyInjection
{
    public static IServiceCollection AddWebServices(this IServiceCollection services, IHostEnvironment environment)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUser>();
        services.AddScoped<IUserClaimsPrincipalFactory<ApplicationUser>, ApplicationUserClaimsPrincipalFactory>();

        services.AddControllersWithViews();
        services.AddFluentValidationAutoValidation();
        services.AddFluentValidationClientsideAdapters();
        services.AddValidatorsFromAssemblyContaining<LoginViewModelValidator>();

        var cookieSecurePolicy = environment.IsDevelopment() || environment.IsEnvironment("Testing")
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;

        services.ConfigureApplicationCookie(options =>
        {
            options.LoginPath = "/Account/Login";
            options.LogoutPath = "/Account/Logout";
            options.AccessDeniedPath = "/Account/AccessDenied";
            options.Cookie.Name = ".ToplandERP.Auth";
            options.Cookie.HttpOnly = true;
            options.Cookie.IsEssential = true;
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.Cookie.SecurePolicy = cookieSecurePolicy;
            options.SlidingExpiration = true;
            options.ExpireTimeSpan = TimeSpan.FromHours(8);
        });

        services.AddAuthorization(options =>
        {
            options.AddPolicy(AuthorizationPolicies.AuthenticatedUser, policy =>
                policy.RequireAuthenticatedUser());

            options.AddPolicy(AuthorizationPolicies.SuperAdmin, policy =>
                policy.RequireRole(RoleNames.SuperAdmin));

            options.AddPolicy(AuthorizationPolicies.CompanyAdmin, policy =>
                policy.RequireRole(RoleNames.SuperAdmin, RoleNames.CompanyAdmin));

            options.AddPolicy(AuthorizationPolicies.SalesEmployee, policy =>
                policy.RequireRole(RoleNames.SuperAdmin, RoleNames.CompanyAdmin, RoleNames.SalesEmployee));

            options.AddPolicy(AuthorizationPolicies.DispatchUser, policy =>
                policy.RequireRole(RoleNames.SuperAdmin, RoleNames.CompanyAdmin, RoleNames.DispatchUser));

            options.AddPolicy(AuthorizationPolicies.CanViewCompanies, policy =>
                policy.RequireAuthenticatedUser());

            options.AddPolicy(AuthorizationPolicies.CanViewCustomers, policy =>
                policy.RequireRole(RoleNames.SuperAdmin, RoleNames.CompanyAdmin, RoleNames.SalesEmployee));

            options.AddPolicy(AuthorizationPolicies.CanManageCustomers, policy =>
                policy.RequireRole(RoleNames.SuperAdmin, RoleNames.CompanyAdmin, RoleNames.SalesEmployee));

            options.AddPolicy(AuthorizationPolicies.CanEditCustomers, policy =>
                policy.RequireRole(RoleNames.SuperAdmin, RoleNames.CompanyAdmin));

            options.AddPolicy(AuthorizationPolicies.CanToggleCustomers, policy =>
                policy.RequireRole(RoleNames.SuperAdmin, RoleNames.CompanyAdmin));

            options.AddPolicy(AuthorizationPolicies.CanViewProducts, policy =>
                policy.RequireRole(RoleNames.SuperAdmin, RoleNames.CompanyAdmin, RoleNames.SalesEmployee));

            options.AddPolicy(AuthorizationPolicies.CanManageProducts, policy =>
                policy.RequireRole(RoleNames.SuperAdmin, RoleNames.CompanyAdmin));

            options.AddPolicy(AuthorizationPolicies.CanViewTransporters, policy =>
                policy.RequireRole(RoleNames.SuperAdmin, RoleNames.CompanyAdmin, RoleNames.SalesEmployee, RoleNames.DispatchUser));

            options.AddPolicy(AuthorizationPolicies.CanManageTransporters, policy =>
                policy.RequireRole(RoleNames.SuperAdmin, RoleNames.CompanyAdmin));

            options.AddPolicy(AuthorizationPolicies.CanViewPaymentConditions, policy =>
                policy.RequireRole(RoleNames.SuperAdmin, RoleNames.CompanyAdmin, RoleNames.SalesEmployee));

            options.AddPolicy(AuthorizationPolicies.CanManagePaymentConditions, policy =>
                policy.RequireRole(RoleNames.SuperAdmin, RoleNames.CompanyAdmin));

            options.AddPolicy(AuthorizationPolicies.CanViewUsers, policy =>
                policy.RequireRole(RoleNames.SuperAdmin, RoleNames.CompanyAdmin));

            options.AddPolicy(AuthorizationPolicies.CanManageUsers, policy =>
                policy.RequireRole(RoleNames.SuperAdmin, RoleNames.CompanyAdmin));

            options.AddPolicy(AuthorizationPolicies.CanViewOrders, policy =>
                policy.RequireRole(RoleNames.SuperAdmin, RoleNames.CompanyAdmin, RoleNames.SalesEmployee, RoleNames.DispatchUser));

            options.AddPolicy(AuthorizationPolicies.CanCreateOrders, policy =>
                policy.RequireRole(RoleNames.SuperAdmin, RoleNames.CompanyAdmin, RoleNames.SalesEmployee));

            options.AddPolicy(AuthorizationPolicies.CanRequestOrderModification, policy =>
                policy.RequireRole(RoleNames.SalesEmployee));

            options.AddPolicy(AuthorizationPolicies.CanReviewOrderModifications, policy =>
                policy.RequireRole(RoleNames.SuperAdmin, RoleNames.CompanyAdmin));

            options.AddPolicy(AuthorizationPolicies.CanMarkReadyToDispatch, policy =>
                policy.RequireRole(RoleNames.SuperAdmin, RoleNames.CompanyAdmin, RoleNames.DispatchUser));

            options.AddPolicy(AuthorizationPolicies.CanDispatchOrders, policy =>
                policy.RequireRole(RoleNames.SuperAdmin, RoleNames.CompanyAdmin, RoleNames.DispatchUser));

            options.FallbackPolicy = options.GetPolicy(AuthorizationPolicies.AuthenticatedUser);
        });

        services.AddAntiforgery(options =>
        {
            options.HeaderName = "X-CSRF-TOKEN";
            options.Cookie.Name = ".ToplandERP.AntiForgery";
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = cookieSecurePolicy;
        });

        return services;
    }
}
