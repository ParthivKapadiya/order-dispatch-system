using Microsoft.Extensions.DependencyInjection;
using ToplandERP.Application.Companies;

namespace ToplandERP.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ICompanyService, CompanyService>();
        return services;
    }
}
