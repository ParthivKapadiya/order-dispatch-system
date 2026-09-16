using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using ToplandERP.Application.Companies;
using ToplandERP.Application.Customers;
using ToplandERP.Application.Dispatching;
using ToplandERP.Application.Modifications;
using ToplandERP.Application.Notifications;
using ToplandERP.Application.Orders;
using ToplandERP.Application.PaymentConditions;
using ToplandERP.Application.Products;
using ToplandERP.Application.Transporters;

namespace ToplandERP.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);
        services.AddScoped<ICompanyService, CompanyService>();
        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<ITransporterService, TransporterService>();
        services.AddScoped<IPaymentConditionService, PaymentConditionService>();
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<IModificationService, ModificationService>();
        services.AddScoped<IDispatchService, DispatchService>();
        services.AddScoped<INotificationService, NotificationService>();
        return services;
    }
}
